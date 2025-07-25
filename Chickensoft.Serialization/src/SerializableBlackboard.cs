namespace Chickensoft.Serialization;

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Chickensoft.Collections;
using Chickensoft.Introspection;

/// <summary>
/// A serializable <see cref="IBlackboard" /> implementation.
/// </summary>
public interface ISerializableBlackboard : IBlackboard {
  /// <summary>
  /// Types that should be persisted when the owning object is serialized.
  /// </summary>
  IEnumerable<Type> SavedTypes { get; }

  /// <summary>
  /// Types that should be saved when the blackboard is serialized. Types are
  /// compared against a stored reference value to determine if they have
  /// changed and should be serialized.
  /// </summary>
  IEnumerable<Type> TypesToSave { get; }

  /// <summary>
  /// Establishes a factory that will be used for the given data type if the
  /// data was not provided during deserialization or if creating a new
  /// instance that has never been serialized.
  /// </summary>
  /// <typeparam name="TData">Type of data to persist.</typeparam>
  /// <param name="factory">Factory closure which creates the data.</param>
  void Save<TData>(Func<TData> factory) where TData : class, IIdentifiable;

  /// <summary>
  /// Establishes a factory that will be used for the given data type if the
  /// data was not provided during deserialization or if creating a new
  /// instance that has never been serialized.
  /// </summary>
  /// <param name="type">Type of data to persist.</param>
  /// <param name="factory">Factory closure which creates the data.</param>
  /// <param name="referenceValue">Reference value to compare against when
  /// deciding to persist the data. If the object is equivalent to this value
  /// when serialization occurs (as determined by the default equality
  /// comparer), the value will not be persisted.</param>
  void SaveObject(
    Type type,
    Func<object> factory,
    object? referenceValue
  );
}

/// <summary>
/// A serializable <see cref="IBlackboard" /> implementation.
/// </summary>
public class SerializableBlackboard :
    Blackboard, ISerializableBlackboard, ICustomSerializable {
  /// <summary>Json property name for blackboard values dictionary.</summary>
  public const string VALUES_PROPERTY = "values";

  /// <summary>
  /// Factory closures that create instances of the expected data types.
  /// </summary>
  protected readonly Dictionary<Type, Func<object>> _saveTypes = [];

  /// <summary>Dictionary that maps save types to a reference value. Used by
  /// <see cref="TypesToSave" /> to only return values that have diverged from
  /// the reference type.</summary>
  protected readonly Dictionary<Type, object?> _referenceValues = [];

  /// <inheritdoc />
  public IEnumerable<Type> SavedTypes => _saveTypes.Keys;

  /// <inheritdoc />
  public IEnumerable<Type> TypesToSave =>
    _saveTypes.Keys.Where(
      k => !SerializationUtilities.IsEquivalent(
        GetObject(k), _referenceValues[k]
      )
    );

  /// <inheritdoc cref="ISerializableBlackboard.Save{TData}(Func{TData})" />
  public void Save<TData>(Func<TData> factory)
    where TData : class, IIdentifiable => SaveObject(
      typeof(TData), factory, null
    );

  /// <inheritdoc
  /// cref="ISerializableBlackboard.SaveObject(Type, Func{object}, object?)" />
  public void SaveObject(
    Type type,
    Func<object> factory,
    object? referenceValue
  ) => SaveObjectData(type, factory, referenceValue);

  /// <summary>
  /// Instantiates and adds any missing saved data types that have not been
  /// added to the blackboard yet.
  /// </summary>
  public void InstantiateAnyMissingSavedData() {
    foreach (var type in _saveTypes.Keys) {
      if (!_blackboard.ContainsKey(type)) {
        _blackboard[type] = _saveTypes[type]();
      }
    }
  }

  /// <inheritdoc
  /// cref="ISerializableBlackboard.SaveObject(Type, Func{object}, object?)" />
  protected virtual void SaveObjectData(
    Type type,
    Func<object> factory,
    object? referenceValue
  ) {
    if (_blackboard.ContainsKey(type)) {
      throw new DuplicateNameException(
        $"Cannot save blackboard data `{type}` since it is already on the " +
        "blackboard."
      );
    }

    _saveTypes[type] = factory;
    _referenceValues[type] = referenceValue;
  }

  /// <inheritdoc />
  protected override object GetBlackboardData(Type type) {
    // If we have data of this type on the blackboard, return it.
    if (_blackboard.TryGetValue(type, out var data)) {
      return data;
    }

    // If it is a persisted type that isn't on the blackboard yet, we can
    // create an instance of the data and add it.
    if (_saveTypes.TryGetValue(type, out var saveType)) {
      data = saveType();
      _blackboard[type] = data;
      return data;
    }

    // We don't have the requested data. Let the original implementation throw.
    return base.GetBlackboardData(type);
  }

  /// <inheritdoc />
  protected override void SetBlackboardData(Type type, object data) {
    if (_saveTypes.ContainsKey(type)) {
      throw new DuplicateNameException(
        $"Cannot set blackboard data `{type}` since it would conflict with " +
        "persisted data on the blackboard."
      );
    }

    base.SetBlackboardData(type, data);
  }

  /// <inheritdoc />
  [UnconditionalSuppressMessage(
    "AOT",
    "IL3050:RequiresDynamicCode",
    Justification = "Chickensoft introspection & serialization system " +
    "ensures compatible types are serializable."
  )]
  [UnconditionalSuppressMessage(
    "AOT",
    "IL2026:RequiresUnreferencedCodeAttribute",
    Justification = "Chickensoft introspection & serialization system " +
    "ensures compatible types are preserved against trimming."
  )]
  public object OnDeserialized(
    IdentifiableTypeMetadata metadata,
    JsonObject json,
    JsonSerializerOptions options
  ) {
    var valuesJson =
      json[VALUES_PROPERTY]?.AsObject() ?? throw new JsonException(
        $"Blackboard is missing the `{VALUES_PROPERTY}` property."
      );

    foreach (var valueJson in valuesJson) {
      var type = Introspection.Types.Graph.GetIdentifiableType(
        id: valueJson.Key,
        version: valueJson.Value?[Serializer.VERSION_PROPERTY]?.GetValue<int>()
      ) ?? throw new JsonException(
        $"Blackboard has an unknown identifiable type id `{valueJson.Key}`."
      );

      var value = JsonSerializer.Deserialize(
        valueJson.Value,
        type,
        options
      ) ?? throw new JsonException(
        $"Failed to deserialize blackboard object `{type}`."
      );

      OverwriteObject(value.GetType(), value);
    }

    return this;
  }

  /// <inheritdoc />
  [UnconditionalSuppressMessage(
    "AOT",
    "IL3050:RequiresDynamicCode",
    Justification = "Chickensoft introspection & serialization system " +
    "ensures compatible types are serializable."
  )]
  [UnconditionalSuppressMessage(
    "AOT",
    "IL2026:RequiresUnreferencedCodeAttribute",
    Justification = "Chickensoft introspection & serialization system " +
    "ensures compatible types are preserved against trimming."
  )]
  public void OnSerialized(
    IdentifiableTypeMetadata metadata,
    JsonObject json,
    JsonSerializerOptions options
  ) {
    var graph = Introspection.Types.Graph;

    var typesToSave = TypesToSave.Select(
      // The Save<T>() method has a constraint on the type that ensures
      // saved types will always be identifiable.
      (type) => new {
        Type = type,
        Metadata = (IdentifiableTypeMetadata)graph.GetMetadata(type)!
      }
    )
    .OrderBy((pair) => pair.Metadata.Id)
    .ThenBy((pair) => pair.Metadata.Version);

    var valuesJson = new JsonObject();

    // Save all the identifiable types we are supposed to persist.
    foreach (var objType in typesToSave) {
      var obj = GetObject(objType.Type);

      valuesJson[objType.Metadata.Id] = JsonSerializer.SerializeToNode(
        value: obj,
        inputType: objType.Type,
        options: options
      );
    }

    json[VALUES_PROPERTY] = valuesJson;
  }
}
