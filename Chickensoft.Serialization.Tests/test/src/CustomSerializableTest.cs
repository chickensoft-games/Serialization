namespace Chickensoft.Serialization.Tests;

using System.Text.Json;
using System.Text.Json.Nodes;
using Chickensoft.Collections;
using Chickensoft.Introspection;
using Shouldly;
using Xunit;

public partial class CustomSerializableTest {
  [Meta, Id("custom_serializable")]
  public partial class CustomSerializable : ICustomSerializable {
    public int Value { get; set; }

    public object OnDeserialized(
      IdentifiableTypeMetadata metadata,
      JsonObject json,
      JsonSerializerOptions options
    ) {
      Value = json["value"]?.GetValue<int>() ?? -1;

      return this;
    }

    public void OnSerialized(
      IdentifiableTypeMetadata metadata,
      JsonObject json,
      JsonSerializerOptions options
    ) {
      // Even though our property doesn't have the [Save] attribute, we
      // can save it manually.
      json["value"] = Value;
    }
  }

  [Fact]
  public void Serializes() {
    var customSerializable = new CustomSerializable {
      Value = 42,
    };

    var options = new JsonSerializerOptions {
      WriteIndented = true,
      TypeInfoResolver = new SerializableTypeResolver(),
      Converters = { new SerializableTypeConverter(new Blackboard()) }
    };

    var json = JsonSerializer.Serialize(customSerializable, options);

    json.ShouldBe(
      /*lang=json,strict*/
      """
      {
        "$type": "custom_serializable",
        "$v": 1,
        "value": 42
      }
      """
    );
  }

  [Fact]
  public void Deserializes() {
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

    var options = new JsonSerializerOptions {
      TypeInfoResolver = new SerializableTypeResolver(),
      Converters = { new SerializableTypeConverter(new Blackboard()) }
    };

    var customSerializable = JsonSerializer.Deserialize<CustomSerializable>(json, options);

    customSerializable.ShouldNotBeNull().Value.ShouldBe(42);
  }
}
