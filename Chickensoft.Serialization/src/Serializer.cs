namespace Chickensoft.Serialization;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Chickensoft.Introspection;

/// <summary>
/// Chickensoft serialization utilities.
/// </summary>
public static class Serializer
{

  /// <summary>
  /// Type discriminator used when serializing and deserializing identifiable
  /// types. Helps with polymorphism.
  /// </summary>
  public const string TYPE_PROPERTY = "$type";

  /// <summary>
  /// Version discriminator used when serializing and deserializing polymorphic
  /// types. Helps with making upgradeable models.
  /// </summary>
  public const string VERSION_PROPERTY = "$v";

  // Stores collection type info factories as they are requested.
  internal static readonly Dictionary<
    Type, Func<JsonSerializerOptions, JsonTypeInfo>
  > _collections = [];

  internal static readonly Dictionary<
    Type, Func<JsonSerializerOptions, JsonTypeInfo>
  > _customConverters = [];

  /// <summary>
  /// Type converter factories for built-in types that System.Text.Json supports
  /// out of the box on every platform.
  /// </summary>
  public static Dictionary<
    Type, Func<JsonSerializerOptions, JsonTypeInfo>
  > BuiltInConverterFactories
  { get; } = new()
  {
    [typeof(bool)] = (options) =>
      JsonMetadataServices.CreateValueInfo<bool>(
        options, JsonMetadataServices.BooleanConverter
      ),
    [typeof(bool?)] = (options) =>
      JsonMetadataServices.CreateValueInfo<bool?>(
        options, JsonMetadataServices.GetNullableConverter<bool>(options)
      ),
    [typeof(byte[])] = (options) =>
      JsonMetadataServices.CreateValueInfo<byte[]>(
        options, JsonMetadataServices.ByteArrayConverter
      ),
    [typeof(byte)] = (options) =>
      JsonMetadataServices.CreateValueInfo<byte>(
        options, JsonMetadataServices.ByteConverter
      ),
    [typeof(byte?)] = (options) =>
      JsonMetadataServices.CreateValueInfo<byte?>(
        options, JsonMetadataServices.GetNullableConverter<byte>(options)
      ),
    [typeof(char)] = (options) =>
      JsonMetadataServices.CreateValueInfo<char>(
        options, JsonMetadataServices.CharConverter
      ),
    [typeof(char?)] = (options) =>
      JsonMetadataServices.CreateValueInfo<char?>(
        options, JsonMetadataServices.GetNullableConverter<char>(options)
      ),
    [typeof(DateTime)] = (options) =>
      JsonMetadataServices.CreateValueInfo<DateTime>(
        options, JsonMetadataServices.DateTimeConverter
      ),
    [typeof(DateTime?)] = (options) =>
      JsonMetadataServices.CreateValueInfo<DateTime?>(
        options, JsonMetadataServices.GetNullableConverter<DateTime>(options)
      ),
    [typeof(DateTimeOffset)] = (options) =>
      JsonMetadataServices.CreateValueInfo<DateTimeOffset>(
        options, JsonMetadataServices.DateTimeOffsetConverter
      ),
    [typeof(DateTimeOffset?)] = (options) =>
      JsonMetadataServices.CreateValueInfo<DateTimeOffset?>(
        options, JsonMetadataServices.GetNullableConverter<DateTimeOffset>(
          options
        )
      ),
    [typeof(decimal)] = (options) =>
      JsonMetadataServices.CreateValueInfo<decimal>(
        options, JsonMetadataServices.DecimalConverter
      ),
    [typeof(decimal?)] = (options) =>
      JsonMetadataServices.CreateValueInfo<decimal?>(
        options, JsonMetadataServices.GetNullableConverter<decimal>(options)
      ),
    [typeof(double)] = (options) =>
      JsonMetadataServices.CreateValueInfo<double>(
        options, JsonMetadataServices.DoubleConverter
      ),
    [typeof(double?)] = (options) =>
      JsonMetadataServices.CreateValueInfo<double?>(
        options, JsonMetadataServices.GetNullableConverter<double>(options)
      ),
    [typeof(Guid)] = (options) =>
      JsonMetadataServices.CreateValueInfo<Guid>(
        options, JsonMetadataServices.GuidConverter
      ),
    [typeof(Guid?)] = (options) =>
      JsonMetadataServices.CreateValueInfo<Guid?>(
        options, JsonMetadataServices.GetNullableConverter<Guid>(options)
      ),
    [typeof(short)] = (options) =>
      JsonMetadataServices.CreateValueInfo<short>(
        options, JsonMetadataServices.Int16Converter
      ),
    [typeof(short?)] = (options) =>
      JsonMetadataServices.CreateValueInfo<short?>(
        options, JsonMetadataServices.GetNullableConverter<short>(options)
      ),
    [typeof(int)] = (options) =>
      JsonMetadataServices.CreateValueInfo<int>(
        options, JsonMetadataServices.Int32Converter
      ),
    [typeof(int?)] = (options) =>
      JsonMetadataServices.CreateValueInfo<int?>(
        options, JsonMetadataServices.GetNullableConverter<int>(options)
      ),
    [typeof(long)] = (options) =>
      JsonMetadataServices.CreateValueInfo<long>(
        options, JsonMetadataServices.Int64Converter
      ),
    [typeof(long?)] = (options) =>
      JsonMetadataServices.CreateValueInfo<long?>(
        options, JsonMetadataServices.GetNullableConverter<long>(options)
      ),
    [typeof(JsonArray)] = (options) =>
      JsonMetadataServices.CreateValueInfo<JsonArray>(
        options, JsonMetadataServices.JsonArrayConverter
      ),
    [typeof(JsonDocument)] = (options) =>
      JsonMetadataServices.CreateValueInfo<JsonDocument>(
        options, JsonMetadataServices.JsonDocumentConverter
      ),
    [typeof(JsonElement)] = (options) =>
      JsonMetadataServices.CreateValueInfo<JsonElement>(
        options, JsonMetadataServices.JsonElementConverter
      ),
    [typeof(JsonNode)] = (options) =>
      JsonMetadataServices.CreateValueInfo<JsonNode>(
        options, JsonMetadataServices.JsonNodeConverter
      ),
    [typeof(JsonObject)] = (options) =>
      JsonMetadataServices.CreateValueInfo<JsonObject>(
        options, JsonMetadataServices.JsonObjectConverter
      ),
    [typeof(JsonValue)] = (options) =>
      JsonMetadataServices.CreateValueInfo<JsonValue>(
        options, JsonMetadataServices.JsonValueConverter
      ),
    [typeof(Memory<byte>)] = (options) =>
      JsonMetadataServices.CreateValueInfo<Memory<byte>>(
        options, JsonMetadataServices.MemoryByteConverter
      ),
    [typeof(Memory<byte>?)] = (options) =>
      JsonMetadataServices.CreateValueInfo<Memory<byte>?>(
        options, JsonMetadataServices.GetNullableConverter<Memory<byte>>(
          options
        )
      ),
    [typeof(object)] = (options) =>
      JsonMetadataServices.CreateValueInfo<object>(
        options, JsonMetadataServices.ObjectConverter
      ),
    [typeof(ReadOnlyMemory<byte>)] = (options) =>
      JsonMetadataServices.CreateValueInfo<ReadOnlyMemory<byte>>(
        options, JsonMetadataServices.ReadOnlyMemoryByteConverter
      ),
    [typeof(ReadOnlyMemory<byte>?)] = (options) =>
      JsonMetadataServices.CreateValueInfo<ReadOnlyMemory<byte>?>(
        options,
        JsonMetadataServices.GetNullableConverter<ReadOnlyMemory<byte>>(
          options
        )
      ),
    [typeof(sbyte)] = (options) =>
      JsonMetadataServices.CreateValueInfo<sbyte>(
        options, JsonMetadataServices.SByteConverter
      ),
    [typeof(sbyte?)] = (options) =>
      JsonMetadataServices.CreateValueInfo<sbyte?>(
        options, JsonMetadataServices.GetNullableConverter<sbyte>(options)
      ),
    [typeof(float)] = (options) =>
      JsonMetadataServices.CreateValueInfo<float>(
        options, JsonMetadataServices.SingleConverter
      ),
    [typeof(float?)] = (options) =>
      JsonMetadataServices.CreateValueInfo<float?>(
        options, JsonMetadataServices.GetNullableConverter<float>(options)
      ),
    [typeof(string)] = (options) =>
      JsonMetadataServices.CreateValueInfo<string>(
        options, JsonMetadataServices.StringConverter
      ),
    [typeof(TimeSpan)] = (options) =>
      JsonMetadataServices.CreateValueInfo<TimeSpan>(
        options, JsonMetadataServices.TimeSpanConverter
      ),
    [typeof(TimeSpan?)] = (options) =>
      JsonMetadataServices.CreateValueInfo<TimeSpan?>(
        options, JsonMetadataServices.GetNullableConverter<TimeSpan>(options)
      ),
    [typeof(ushort)] = (options) =>
      JsonMetadataServices.CreateValueInfo<ushort>(
        options, JsonMetadataServices.UInt16Converter
      ),
    [typeof(ushort?)] = (options) =>
      JsonMetadataServices.CreateValueInfo<ushort?>(
        options, JsonMetadataServices.GetNullableConverter<ushort>(options)
      ),
    [typeof(uint)] = (options) =>
      JsonMetadataServices.CreateValueInfo<uint>(
        options, JsonMetadataServices.UInt32Converter
      ),
    [typeof(uint?)] = (options) =>
      JsonMetadataServices.CreateValueInfo<uint?>(
        options, JsonMetadataServices.GetNullableConverter<uint>(options)
      ),
    [typeof(ulong)] = (options) =>
      JsonMetadataServices.CreateValueInfo<ulong>(
        options, JsonMetadataServices.UInt64Converter
      ),
    [typeof(ulong?)] = (options) =>
      JsonMetadataServices.CreateValueInfo<ulong?>(
        options, JsonMetadataServices.GetNullableConverter<ulong>(options)
      ),
    [typeof(Uri)] = (options) =>
      JsonMetadataServices.CreateValueInfo<Uri>(
        options, JsonMetadataServices.UriConverter
      ),
    [typeof(Version)] = (options) =>
      JsonMetadataServices.CreateValueInfo<Version>(
        options, JsonMetadataServices.VersionConverter
      )
  };

  internal static bool _isInitialized;

  [ModuleInitializer]
  internal static void Initialize()
  {
    if (_isInitialized)
    {
      return;
    }

    Types.Graph.AddCustomType(
      type: typeof(SerializableBlackboard),
      name: "SerializableBlackboard",
      genericTypeGetter: (r) => r.Receive<SerializableBlackboard>(),
      factory: () => new SerializableBlackboard(),
      id: "blackboard",
      version: 1
    );

    _isInitialized = true;

    return;
  }

  /// <summary>
  /// Adds a custom converter for a type that is outside the current assembly.
  /// </summary>
  /// <param name="converter">Custom converter.</param>
  /// <typeparam name="T">Type of value to convert.</typeparam>
  public static void AddConverter<T>(JsonConverter<T> converter) =>
    _customConverters[typeof(T)] = (options) =>
    {
      var expandedConverter = ExpandConverter(typeof(T), converter, options);

      return JsonMetadataServices.CreateValueInfo<T>(
        options, expandedConverter
      );
    };

  internal static void RemoveConverter<T>() =>
    _customConverters.Remove(typeof(T));

  #region Private Helper Types
  // Call with list element type
  private class ListInfoCreator : ITypeReceiver
  {
    public JsonSerializerOptions Options { get; }
    public JsonTypeInfo TypeInfo { get; private set; } = default!;

    public ListInfoCreator(JsonSerializerOptions options)
    {
      Options = options;
    }

    public void Receive<T>()
    {
      var info = new JsonCollectionInfoValues<List<T>>()
      {
        ObjectCreator = () => [],
        SerializeHandler = null
      };
      TypeInfo = JsonMetadataServices.CreateListInfo<List<T>, T>(Options, info);
      TypeInfo.NumberHandling = null;
    }
  }

  // Call with hash set element type
  private class HashSetInfoCreator : ITypeReceiver
  {
    public JsonSerializerOptions Options { get; }
    public JsonTypeInfo TypeInfo { get; private set; } = default!;

    public HashSetInfoCreator(JsonSerializerOptions options)
    {
      Options = options;
    }

    public void Receive<T>()
    {
      var info = new JsonCollectionInfoValues<HashSet<T>>()
      {
        ObjectCreator = () => [],
        SerializeHandler = null
      };
      TypeInfo = JsonMetadataServices.CreateISetInfo<HashSet<T>, T>(
        Options, info
      );
      TypeInfo.NumberHandling = null;
    }
  }

  // Call with dictionary key and value types
  private class DictionaryInfoCreator : ITypeReceiver2
  {
    public JsonSerializerOptions Options { get; }
    public JsonTypeInfo TypeInfo { get; private set; } = default!;

    public DictionaryInfoCreator(JsonSerializerOptions options)
    {
      Options = options;
    }

    public void Receive<TA, TB>()
    {
#pragma warning disable CS8714
      var info = new JsonCollectionInfoValues<Dictionary<TA, TB>>()
      {
        ObjectCreator = () => [],
        SerializeHandler = null
      };
      TypeInfo = JsonMetadataServices.CreateDictionaryInfo<
        Dictionary<TA, TB>, TA, TB
      >(Options, info);
#pragma warning restore CS8714
      TypeInfo.NumberHandling = null;
    }
  }

  internal class CustomConverterTypeInfoCreator : ITypeReceiver
  {
    public JsonSerializerOptions Options { get; }
    public JsonConverter Converter { get; }
    public JsonTypeInfo TypeInfo { get; private set; } = default!;

    public CustomConverterTypeInfoCreator(
      JsonSerializerOptions options,
      JsonConverter converter
    )
    {
      Options = options;
      Converter = converter;
    }

    public void Receive<T>() =>
      TypeInfo = JsonMetadataServices.CreateValueInfo<T>(Options, Converter);
  }
  #endregion Private Helper Types

  #region Private Methods
  // Recursively identify collection types described by the introspection data
  // for a generic member type.

  /// <summary>
  /// Recursively identifies and caches collection types described by the given
  /// generated generic type information.
  /// </summary>
  /// <param name="genericType">Generic type description.</param>
  /// <param name="resolver">Originating type resolver, if any.</param>
  /// <param name="options">Serialization options.</param>
  public static void IdentifyCollectionTypes(
    TypeNode genericType,
    IJsonTypeInfoResolver? resolver,
    JsonSerializerOptions options
  )
  {
    if (_collections.ContainsKey(genericType.ClosedType))
    {
      // We've already cached this collection type.
      return;
    }

    if (genericType.OpenType == typeof(List<>))
    {
      _collections[genericType.ClosedType] = (options) =>
      {
        var listInfoCreator = new ListInfoCreator(options);
        genericType.Arguments[0].GenericTypeGetter(listInfoCreator);
        var typeInfo = listInfoCreator.TypeInfo;
        typeInfo.OriginatingResolver = resolver;
        return typeInfo;
      };

      IdentifyCollectionTypes(genericType.Arguments[0], resolver, options);
    }
    else if (genericType.OpenType == typeof(HashSet<>))
    {
      _collections[genericType.ClosedType] = (options) =>
      {
        var hashSetInfoCreator = new HashSetInfoCreator(options);
        genericType.Arguments[0].GenericTypeGetter(hashSetInfoCreator);
        var typeInfo = hashSetInfoCreator.TypeInfo;
        typeInfo.OriginatingResolver = resolver;
        return typeInfo;
      };

      IdentifyCollectionTypes(genericType.Arguments[0], resolver, options);
    }
    else if (genericType.OpenType == typeof(Dictionary<,>))
    {
      _collections[genericType.ClosedType] = (options) =>
      {
        var dictionaryInfoCreator = new DictionaryInfoCreator(options);
        genericType.GenericTypeGetter2!(dictionaryInfoCreator);
        var typeInfo = dictionaryInfoCreator.TypeInfo;
        typeInfo.OriginatingResolver = resolver;
        return typeInfo;
      };

      IdentifyCollectionTypes(genericType.Arguments[0], resolver, options);
      IdentifyCollectionTypes(genericType.Arguments[1], resolver, options);
    }
  }

  internal static JsonConverter? GetRuntimeConverterForType(
    Type type, JsonSerializerOptions options
  )
  {
    for (var i = 0; i < options.Converters.Count; i++)
    {
      var converter = options.Converters[i];
      if (converter.CanConvert(type))
      {
        return ExpandConverter(type, converter, options);
      }
    }

    return null;
  }

  [return: NotNullIfNotNull(nameof(converter))]
  internal static JsonConverter? ExpandConverter(
    Type type,
    JsonConverter? converter,
    JsonSerializerOptions options
  )
  {
    if (converter is JsonConverterFactory factory)
    {
      converter = factory.CreateConverter(type, options);
      if (converter is null or JsonConverterFactory)
      {
        throw new InvalidOperationException(string.Format(
          "The converter '{0}' cannot return null or a " +
          "JsonConverterFactory instance.",
          factory.GetType()
        ));
      }
    }

    return converter;
  }
  #endregion Private Methods
}
