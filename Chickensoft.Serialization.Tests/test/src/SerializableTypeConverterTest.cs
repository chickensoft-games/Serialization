namespace Chickensoft.Serialization.Tests;

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Text.Json;
using Chickensoft.Collections;
using Chickensoft.Introspection;
using Chickensoft.Serialization.Tests.Fixtures;
using Shouldly;
using Xunit;

[
  SuppressMessage(
    "Performance",
    "CA1869",
    Justification = "We want new JsonSerializerOptions for each test"
  )
]
public partial class SerializableTypeConverterTest
{
  [Fact]
  public void IdentifiesCollections()
  {
    SerializableTypeConverter.IsCollection(typeof(List<>)).ShouldBeTrue();
    SerializableTypeConverter.IsCollection(typeof(Dictionary<,>)).ShouldBeTrue();
    SerializableTypeConverter.IsCollection(typeof(HashSet<>)).ShouldBeTrue();
    SerializableTypeConverter.IsCollection(typeof(object)).ShouldBeFalse();
  }

  [Fact]
  public void Initializes()
  {
    new SerializableTypeConverter().ShouldNotBeNull();

    var blackboard = new Blackboard();
    new SerializableTypeConverter(blackboard)
      .DependenciesBlackboard.ShouldBeSameAs(blackboard);
  }

  [Fact]
  public void SerializesAndDeserializes()
  {
    var person = new Person
    {
      Name = "John Doe",
      Age = 30,
      Pet = new Dog
      {
        Name = "Fido",
        BarkVolume = 11,
      },
    };

    var options = new JsonSerializerOptions
    {
      WriteIndented = true,
      TypeInfoResolver = new SerializableTypeResolver(),
      Converters = { new SerializableTypeConverter(new Blackboard()) }
    };

    var json = JsonSerializer.Serialize(person, options);

    json.ShouldBe(
      /*lang=json,strict*/
      """
      {
        "$type": "person",
        "$v": 1,
        "age": 30,
        "name": "John Doe",
        "pet": {
          "$type": "dog",
          "$v": 1,
          "bark_volume": 11,
          "name": "Fido"
        }
      }
      """,
      options: StringCompareShould.IgnoreLineEndings
    );

    var deserializedPerson =
      JsonSerializer.Deserialize<Person>(json, options);

    deserializedPerson.ShouldBe(person);
  }

  [Fact]
  public void InitPropertiesSerialize()
  {
    var model = new InitPropertyModel()
    {
      Name = "Jane Doe",
      Age = 30,
      Descriptions = [
        "One",
        "Two",
        "Three"
      ]
    };

    var options = new JsonSerializerOptions
    {
      WriteIndented = true,
      TypeInfoResolver = new SerializableTypeResolver(),
      Converters = { new SerializableTypeConverter(new Blackboard()) }
    };

    var json = JsonSerializer.Serialize(model, options);

    json.ShouldBe(
      /*lang=json,strict*/
      """
      {
        "$type": "init_property_model",
        "$v": 1,
        "age": 30,
        "descriptions": [
          "One",
          "Two",
          "Three"
        ],
        "name": "Jane Doe"
      }
      """,
      options: StringCompareShould.IgnoreLineEndings
    );

    var deserializedModel =
      JsonSerializer.Deserialize<InitPropertyModel>(json, options);

    deserializedModel.ShouldBeEquivalentTo(model);
  }

  [Fact]
  public void ThrowsIfTryingToWriteNonIdentifiableType()
  {
    var options = new JsonSerializerOptions
    {
      WriteIndented = true,
      TypeInfoResolver = new SerializableTypeResolver()
    };
    var converter = new SerializableTypeConverter(new Blackboard());
    options.Converters.Add(converter);

    var writer = new Utf8JsonWriter(
      new MemoryStream(),
      new JsonWriterOptions { Indented = true }
    );

    Should.Throw<JsonException>(
      () => converter.Write(writer, new object(), options)
    );
  }

  [Fact]
  public void ThrowsIfTryingToReadNonObject()
  {
    var options = new JsonSerializerOptions
    {
      WriteIndented = true,
      TypeInfoResolver = new SerializableTypeResolver()
    };
    var converter = new SerializableTypeConverter(new Blackboard());
    options.Converters.Add(converter);

    Should.Throw<JsonException>(
      () =>
      {
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes("null"));
        converter.Read(ref reader, typeof(object), options);
      }
    );
  }

  [Fact]
  public void ThrowsIfMissingTypeDiscriminator()
  {
    var options = new JsonSerializerOptions
    {
      WriteIndented = true,
      TypeInfoResolver = new SerializableTypeResolver()
    };
    var converter = new SerializableTypeConverter(new Blackboard());
    options.Converters.Add(converter);

    Should.Throw<JsonException>(
      () =>
      {
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes("{}"));
        converter.Read(ref reader, typeof(object), options);
      }
    );
  }

  [Fact]
  public void ThrowsIfUnknownType()
  {
    var options = new JsonSerializerOptions
    {
      WriteIndented = true,
      TypeInfoResolver = new SerializableTypeResolver()
    };
    var converter = new SerializableTypeConverter(new Blackboard());
    options.Converters.Add(converter);

    Should.Throw<JsonException>(
      () =>
      {
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(
          /*lang=json*/
          @"{""$type"":""unknown"",""$v"":1}"
        ));
        converter.Read(ref reader, typeof(object), options);
      }
    );
  }

  [Fact]
  public void ThrowsIfTryingToDeserializeNonConcreteModel()
  {
    var options = new JsonSerializerOptions
    {
      WriteIndented = true,
      TypeInfoResolver = new SerializableTypeResolver()
    };
    var converter = new SerializableTypeConverter(new Blackboard());
    options.Converters.Add(converter);

    Should.Throw<JsonException>(
      () =>
      {
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(
          /*lang=json*/
          """
          {
            "$type": "non_concrete_model",
            "$v": 1
          }
          """
        ));
        converter.Read(ref reader, typeof(object), options);
      }
    );
  }

  [Fact]
  public void SkipsPropertyWithoutSetter()
  {
    var options = new JsonSerializerOptions
    {
      WriteIndented = true,
      TypeInfoResolver = new SerializableTypeResolver(),
      Converters = { new SerializableTypeConverter(new Blackboard()) }
    };

    var json =
      /*lang=json,strict*/
      """
      {
        "$type": "no_setter_model",
        "$v": 1,
        "name": "Other"
      }
      """;

    var model = JsonSerializer.Deserialize<NoSetterModel>(json, options);

    model.ShouldNotBeNull().Name.ShouldBe("Model");
  }

  [Fact]
  public void UpgradesOutdatedVersionsOnDeserialization()
  {
    var options = new JsonSerializerOptions
    {
      WriteIndented = true,
      TypeInfoResolver = new SerializableTypeResolver(),
      Converters = { new SerializableTypeConverter(new Blackboard()) }
    };

    var json =
    /*lang=json,strict*/
    """
    {
      "$type": "versioned_model",
      "$v": 1
    }
    """;

    var model = JsonSerializer.Deserialize<VersionedModel>(json, options);

    model.ShouldNotBeNull().ShouldBeOfType<VersionedModel3>();
  }

  [Meta, Id("non_concrete_model")]
  public abstract partial class NonConcreteModel;

  [Meta, Id("no_setter_model")]
  public partial class NoSetterModel
  {
    [Save("name")]
    public string Name { get; } = "Model";
  }

  [Meta, Id("log_entry")]
  public abstract partial class BaseLogEntry { }

  [Meta, Version(1)]
  public partial class LogEntry : BaseLogEntry, IOutdated
  {
    [Save("text")]
    public required string Text { get; init; }

    [Save("type")]
    public required string Type { get; init; }

    public object Upgrade(IReadOnlyBlackboard deps) => new LogEntry2()
    {
      Text = Text,
      Type = Type switch
      {
        "info" => LogType.Info,
        "warning" => LogType.Warning,
        "error" or _ => LogType.Error,
      }
    };
  }

  public enum LogType
  {
    Info,
    Warning,
    Error
  }

  [Meta, Version(2)]
  public partial class LogEntry2 : BaseLogEntry
  {
    [Save("text")]
    public required string Text { get; init; }

    [Save("type")]
    public required LogType Type { get; init; }
  }
}
