namespace Chickensoft.Serialization.Tests;

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;
using Chickensoft.Collections;
using Chickensoft.Introspection;
using Shouldly;
using Xunit;

[
  SuppressMessage(
    "Performance",
    "CA1869",
    Justification = "We want new JsonSerializerOptions for each test"
  )
]
public partial class CustomSerializableTest
{
  [Meta, Id("custom_serializable")]
  public partial class CustomSerializable : ICustomSerializable
  {
    public int Value { get; set; }

    public object OnDeserialized(
      IdentifiableTypeMetadata metadata,
      JsonObject json,
      JsonSerializerOptions options
    )
    {
      Value = json["value"]?.GetValue<int>() ?? -1;

      return this;
    }

    public void OnSerialized(
      IdentifiableTypeMetadata metadata,
      JsonObject json,
      JsonSerializerOptions options
    ) =>
      // Even though our property doesn't have the [Save] attribute, we
      // can save it manually.
      json["value"] = Value;
  }

  [Fact]
  public void Serializes()
  {
    var customSerializable = new CustomSerializable
    {
      Value = 42,
    };

    var options = new JsonSerializerOptions
    {
      WriteIndented = true,
      TypeInfoResolver = new SerializableTypeResolver(),
      Converters = { new SerializableTypeConverter(new Blackboard()) }
    };

    var json = JsonSerializer.Serialize(customSerializable, options);
    var jsonNode = JsonNode.Parse(json);

    var expectedJson = /*lang=json,strict*/
      """
      {
        "$type": "custom_serializable",
        "$v": 1,
        "value": 42
      }
      """;
    var expectedJsonNode = JsonNode.Parse(expectedJson);

    JsonNode.DeepEquals(jsonNode, expectedJsonNode).ShouldBeTrue();
  }

  [Fact]
  public void Deserializes()
  {
    var json = JsonNode.Parse(
      /*lang=json,strict*/
      """
      {
        "$type": "custom_serializable",
        "$v": 1,
        "value": 42
      }
      """
    );

    var options = new JsonSerializerOptions
    {
      TypeInfoResolver = new SerializableTypeResolver(),
      Converters = { new SerializableTypeConverter(new Blackboard()) }
    };

    var customSerializable = json.Deserialize<CustomSerializable>(options);

    customSerializable.ShouldNotBeNull().Value.ShouldBe(42);
  }
}
