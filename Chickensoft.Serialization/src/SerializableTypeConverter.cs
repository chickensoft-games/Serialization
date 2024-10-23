namespace Chickensoft.Serialization;

using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Chickensoft.Collections;
using Chickensoft.Introspection;
using System.Collections.Generic;

/// <summary>
/// Serializable type converter that upgrades outdated serializable types
/// as soon as they are deserialized.
/// </summary>
public interface ISerializableTypeConverter {
  /// <summary>
  /// Dependencies that outdated serializable types might need after being
  /// deserialized to upgrade themselves.
  /// </summary>
  public IReadOnlyBlackboard DependenciesBlackboard { get; }
}

/// <inheritdoc />
public class SerializableTypeConverter :
JsonConverter<object>, ISerializableTypeConverter {

  private class NullableTypeReceiver : ITypeReceiver {
    public Type NullableType { get; private set; } = default!;

    // silly, but works for both reference and value types amazingly
    public void Receive<T>() {
      NullableType = typeof(T);
    }
  }

  [ThreadStatic]
  private static NullableTypeReceiver _nullTypeMaker = default!;

  /// <inheritdoc />
  public IReadOnlyBlackboard DependenciesBlackboard { get; }

  internal static ITypeGraph DefaultGraph => Types.Graph;
  // Graph to use for introspection. Allows it to be shimmed for testing.
  internal static ITypeGraph Graph { get; set; } = DefaultGraph;

  private string TypeDiscriminator => Serializer.TYPE_PROPERTY;
  private string VersionDiscriminator => Serializer.VERSION_PROPERTY;

  /// <summary>
  /// Create a new logic block converter with the given type info resolver.
  /// </summary>
  /// <param name="dependenciesBlackboard">Dependencies that might be needed
  /// by outdated states to upgrade themselves.</param>
  public SerializableTypeConverter(
    IReadOnlyBlackboard? dependenciesBlackboard = null
  ) {
    DependenciesBlackboard = dependenciesBlackboard ?? new Blackboard();
  }

  /// <inheritdoc />
  public override bool CanConvert(Type typeToConvert) =>
    Graph.GetMetadata(typeToConvert) is IIntrospectiveTypeMetadata;

  /// <inheritdoc />
  public override object? Read(
    ref Utf8JsonReader reader,
    Type typeToConvert,
    JsonSerializerOptions options
  ) {
    _nullTypeMaker ??= new NullableTypeReceiver();

    var json = JsonNode.Parse(ref reader)?.AsObject() ?? throw new JsonException(
      $"Failed to parse JSON for introspective type {typeToConvert}."
    );

    var typeId =
      json[TypeDiscriminator]?.ToString() ?? throw new JsonException(
        $"Type {typeToConvert} is missing the `{TypeDiscriminator}` type " +
        "discriminator."
      );

    var version =
      json[VersionDiscriminator]?.GetValue<int>() ?? throw new JsonException(
        $"Type {typeToConvert} is missing the `{VersionDiscriminator}` " +
        "version discriminator."
      );

    if (
      Graph.GetIdentifiableType(typeId, version) is not { } type ||
      Graph.GetMetadata(type) is not IdentifiableTypeMetadata metadata
    ) {
      throw new JsonException(
        $"The type `{typeToConvert}` has an unknown identifiable type: " +
        $"id = {typeId}, version = {version}."
      );
    }

    // Get all serializable properties, including those from base types.
    var properties = Graph.GetProperties(type);

    var hasInitProps = metadata.Metatype.HasInitProperties;

    // Create an instance of the type using the generated factory if
    // it does not have init props.
    var value = hasInitProps ? null : metadata.Factory();

    var initProps = hasInitProps ? new Dictionary<string, object?>() : null;
    var normalProps = hasInitProps ? new List<Action>() : null;

    foreach (var property in properties) {
      if (GetPropertyId(property) is not { } propertyId) {
        // Only read properties marked with the [Save] attribute.
        continue;
      }

      // If the property is a collection type, we need to make sure we've
      // cached the closed type of the collection type (recursively) before
      // trying to deserialize it.
      Serializer.IdentifyCollectionTypes(
        property.TypeNode,
        options.TypeInfoResolver,
        options
      );

      var isPresentInJson = json.ContainsKey(propertyId);
      var propertyValueJsonNode = isPresentInJson ? json[propertyId] : null;

      object? propertyValue = null;

      var propertyType = property.TypeNode.ClosedType;

      if (isPresentInJson) {
        // Peek at the type of the property's value to see if it's more
        // specific than the type in the metadata (i.e., a derived type).
        // This allows us to support concrete implementations of declared types
        // for properties that are interfaces or abstract classes.

        if (
          propertyValueJsonNode is JsonObject propertyJsonObj &&
          propertyValueJsonNode[TypeDiscriminator]?.ToString()
            is { } propertyTypeId &&
          Graph.GetIdentifiableType(propertyTypeId) is { } idType
        ) {
          // Peeking a property value's type only works if the property value
          // is a non-null object and it actually has a type discriminator.
          // Types with System.Text.Json generated metadata won't necessarily
          // have type discriminators or may have a different field name for the
          // type discriminator, ensuring those will still be handled by STJ
          // itself.
          //
          // Update known type to be the more specific type.
          propertyType = idType;
        }

        if (propertyType.IsValueType && property.TypeNode.IsNullable) {
          // nullable value types
          property.TypeNode.GenericTypeGetter(_nullTypeMaker);
          propertyType = _nullTypeMaker.NullableType!;
        }

        propertyValue = JsonSerializer.Deserialize(
          propertyValueJsonNode,
          propertyType,
          options
        );
      }

      var shouldSet = isPresentInJson;

      if (
        !isPresentInJson &&
        IsCollection(property.TypeNode.OpenType) &&
        !property.HasDefaultValue
      ) {
        // Property is not in the json, but it's a collection value that doesn't
        // have a default value in the model.
        //
        // In this scenario, we actually prefer an empty collection. We only
        // deserialize a collection to null if it doesn't have a setter or
        // if it's present in the json *and* explicitly set to null.
        //
        // We know we've discovered the collection type already, so it will
        // have type info. Also, we expect the type resolver to exist and be
        // a SerializableTypeResolver that provides our cached collection type
        // info.
        var typeInfo =
          options
            .TypeInfoResolver!
            .GetTypeInfo(propertyType, options)!;

        // Our type resolver companion will have cached the closed type of
        // the collection type by using the callbacks provided in the generated
        // introspection data, which is AOT-friendly :D
        propertyValue = typeInfo.CreateObject!();

        // We want to set the property to the empty collection.
        shouldSet = true;
      }

      if (!shouldSet) {
        continue;
      }

      if (hasInitProps) {
        // Init properties require us to set properties later, so we save
        // the init prop values to use in the generated metadata constructor.
        //
        // We also save closures which will set the normal properties, too.
        if (property.IsInit) {
          initProps!.Add(property.Name, propertyValue);
        }
        else if (property.Setter is { } propertySetter) {
          normalProps!.Add(() => propertySetter(value!, propertyValue));
        }
      }
      else if (property.Setter is { } propertySetter) {
        // We can set the property immediately since there are no init props.
        propertySetter(value!, propertyValue);
      }
    }

    // We have to use the generated metatype method to construct objects with
    // init properties.
    if (hasInitProps) {
      value = metadata.Metatype.Construct(initProps);

      // Set other properties that are not init properties now that we have
      // an object.
      foreach (var setProp in normalProps!) {
        setProp();
      }
    }

    // Upgrade the deserialized object as needed.
    while (value is IOutdated outdated) {
      value = outdated.Upgrade(DependenciesBlackboard);
    }

    // At this point, we've successfully deserialized a type and its properties.
    // If the type implements ISerializationAware, we'll call the OnDeserialized
    // method to allow it to modify itself (or replace itself altogether) based
    // on the json object data.
    if (value is ICustomSerializable aware) {
      // We know the type must be concrete and identifiable at this point.
      value = aware.OnDeserialized(metadata, json, options);
    }

    return value;
  }

  /// <inheritdoc />
  public override void Write(
    Utf8JsonWriter writer,
    object value,
    JsonSerializerOptions options
  ) {
    _nullTypeMaker ??= new NullableTypeReceiver();

    var type = value.GetType();

    var json = new JsonObject();

    var metadata = Graph.GetMetadata(type);

    if (
      metadata is not IIdentifiableTypeMetadata idMetadata ||
      metadata is not IConcreteIntrospectiveTypeMetadata concreteMetadata
    ) {
      throw new JsonException(
        $"The type `{type}` is not an identifiable introspective type."
      );
    }

    var typeId = idMetadata.Id;
    var version = concreteMetadata.Version;

    json[TypeDiscriminator] = typeId;
    json[VersionDiscriminator] = version;

    // Get all serializable properties, including those from base types.
    var properties = Graph.GetProperties(type);

    foreach (var property in properties) {
      if (property.Getter is not { } getter) {
        // Property cannot be read, only set.
        continue;
      }

      if (GetPropertyId(property) is not { } propertyId) {
        // Only write properties marked with the [Save] attribute.
        continue;
      }

      // If the property is a collection type, we need to make sure we've
      // cached the closed type of the collection type (recursively) before
      // trying to serialize it.
      Serializer.IdentifyCollectionTypes(
        property.TypeNode,
        options.TypeInfoResolver,
        options
      );

      var propertyValue = getter(value);
      var valueType = propertyValue?.GetType();
      var propertyType = property.TypeNode.ClosedType;

      if (
        valueType is { } &&
        options.TypeInfoResolver!.GetTypeInfo(valueType, options) is { }
      ) {
        // The actual instance type is a known serializable type, so we assume
        // it is more specific than the declared property type. Use it instead.
        propertyType = valueType;
      }

      if (propertyType.IsValueType && property.TypeNode.IsNullable) {
        // nullable value types
        property.TypeNode.GenericTypeGetter(_nullTypeMaker);
        propertyType = _nullTypeMaker.NullableType!;
      }

      json[propertyId] = JsonSerializer.SerializeToNode(
        value: propertyValue,
        inputType: propertyType,
        options: options
      );
    }

    // We've constructed the json data and we're about to write it to the
    // Utf8JsonWriter. If the type implements ISerializationAware, we'll call
    // the OnSerialized method to allow it to modify the json object data
    // before we actually output it.
    if (
      value is ICustomSerializable aware &&
      metadata is IdentifiableTypeMetadata identifiableTypeMetadata
    ) {
      aware.OnSerialized(identifiableTypeMetadata, json, options);
    }

    json.WriteTo(writer);
  }

  internal static string? GetPropertyId(PropertyMetadata property) =>
    property
      .Attributes
      .TryGetValue(typeof(SaveAttribute), out var saveAttributes) &&
    saveAttributes is { Length: > 0 } &&
    saveAttributes[0] is SaveAttribute saveAttribute
      ? saveAttribute.Id
      : null;

  internal static bool IsCollection(Type openType) =>
    openType == typeof(List<>) ||
    openType == typeof(HashSet<>) ||
    openType == typeof(Dictionary<,>);
}
