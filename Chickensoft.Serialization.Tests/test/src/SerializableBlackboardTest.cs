namespace Chickensoft.Serialization.Tests;

using System.Collections.Generic;
using System.Data;
using System.Text.Json;
using System.Text.Json.Nodes;
using Chickensoft.Collections;
using Chickensoft.Introspection;
using Shouldly;
using Xunit;

public partial class SerializableBlackboardTest {
  [Meta, Id("serializable_blackboard_test_object")]
  public partial record TestObject() {
    [Save("value")]
    public int Value { get; init; }
  }

  [Fact]
  public void Save() {
    var blackboard = new SerializableBlackboard();
    var obj = new TestObject() { Value = 1 };
    blackboard.Save(() => obj);
    blackboard.Get<TestObject>().ShouldBeSameAs(obj);
    blackboard.Get<TestObject>()
      .ShouldBeEquivalentTo(new TestObject() { Value = 1 });
    blackboard.SavedTypes.ShouldContain(typeof(TestObject));
  }

  [Fact]
  public void SaveObject() {
    var blackboard = new SerializableBlackboard();
    var obj = new TestObject() { Value = 1 };
    blackboard.SaveObject(
      type: typeof(TestObject),
      factory: () => obj,
      referenceValue: new TestObject() { Value = 2 }
    );
    blackboard.Get<TestObject>().ShouldNotBeNull();
    blackboard.SavedTypes.ShouldContain(typeof(TestObject));
    blackboard.TypesToSave.ShouldContain(typeof(TestObject));
  }

  [Fact]
  public void TypesToSaveDoesNotHaveObjectIfEquivalent() {
    var blackboard = new SerializableBlackboard();
    var obj = new TestObject() { Value = 1 };
    blackboard.SaveObject(
      type: typeof(TestObject),
      factory: () => obj,
      referenceValue: new TestObject() { Value = 1 }
    );
    blackboard.Get<TestObject>().ShouldNotBeNull();
    blackboard.SavedTypes.ShouldContain(typeof(TestObject));
    blackboard.TypesToSave.ShouldNotContain(typeof(TestObject));
  }

  [Fact]
  public void InstantiatesAnyMissingSavedData() {
    var blackboard = new SerializableBlackboard();

    var obj = new TestObject() { Value = 1 };

    blackboard.Save(() => obj);

    blackboard.Has<TestObject>().ShouldBeFalse();

    blackboard.InstantiateAnyMissingSavedData();

    blackboard.Has<TestObject>().ShouldBeTrue();
  }

  [Fact]
  public void ThrowsIfSavingDataTypeAlreadyOnBlackboard() {
    var blackboard = new SerializableBlackboard();
    var obj = new TestObject() { Value = 1 };
    var obj2 = new TestObject() { Value = 2 };

    blackboard.Set(obj2);

    Should.Throw<DuplicateNameException>(() => blackboard.Save(() => obj));
  }

  [Fact]
  public void ThrowsIfTypeNotFound() {
    var blackboard = new SerializableBlackboard();
    var obj = new TestObject() { Value = 1 };

    Should.Throw<KeyNotFoundException>(blackboard.Get<string>);
  }

  [Fact]
  public void CannotSetValueIfGoingToBeSaved() {
    var blackboard = new SerializableBlackboard();
    var obj = new TestObject() { Value = 1 };

    blackboard.Save(() => obj);

    Should.Throw<DuplicateNameException>(() => blackboard.Set(obj));
  }

  [Fact]
  public void Serializes() {
    var options = new JsonSerializerOptions {
      WriteIndented = true,
      TypeInfoResolver = new SerializableTypeResolver(),
      Converters = { new SerializableTypeConverter(new Blackboard()) }
    };

    var blackboard = new SerializableBlackboard();

    var obj = new TestObject() { Value = 42 };

    blackboard.Save(() => obj);

    var json = JsonSerializer.Serialize(blackboard, options);
    var jsonNode = JsonNode.Parse(json);

    var expectedJson = /*lang=json,strict*/
      """
      {
        "$type": "blackboard",
        "$v": 1,
        "values": {
          "serializable_blackboard_test_object": {
            "$type": "serializable_blackboard_test_object",
            "$v": 1,
            "value": 42
          }
        }
      }
      """;
    var expectedJsonNode = JsonNode.Parse(expectedJson);

    JsonNode.DeepEquals(jsonNode, expectedJsonNode).ShouldBeTrue();

    var deserialized = JsonSerializer.Deserialize<SerializableBlackboard>(
      json,
      options
    );

    deserialized.ShouldNotBeNull().Get<TestObject>().ShouldBeEquivalentTo(obj);
  }

  [Fact]
  public void DeserializationFailsIfNoValuesProperty() {
    var options = new JsonSerializerOptions {
      WriteIndented = true,
      TypeInfoResolver = new SerializableTypeResolver(),
      Converters = { new SerializableTypeConverter(new Blackboard()) }
    };

    var json = /*lang=json,strict*/
      """
      {
        "$type": "blackboard",
        "$v": 1
      }
      """;

    Should.Throw<JsonException>(
      () => JsonSerializer.Deserialize<SerializableBlackboard>(json, options)
    );
  }

  [Fact]
  public void DeserializationThrowsIfValueHasUnknownIdentifiableType() {
    var options = new JsonSerializerOptions {
      WriteIndented = true,
      TypeInfoResolver = new SerializableTypeResolver(),
      Converters = { new SerializableTypeConverter(new Blackboard()) }
    };

    var json = /*lang=json,strict*/
      """
      {
        "$type": "blackboard",
        "$v": 1,
        "values": {
          "unknown": {
            "$type": "unknown",
            "$v": 1
          }
        }
      }
      """;

    Should.Throw<JsonException>(
      () => JsonSerializer.Deserialize<SerializableBlackboard>(json, options)
    );
  }

  [Fact]
  public void DeserializationThrowsIfValueHasUnknownVersion() {
    var options = new JsonSerializerOptions {
      WriteIndented = true,
      TypeInfoResolver = new SerializableTypeResolver(),
      Converters = { new SerializableTypeConverter(new Blackboard()) }
    };

    var json = /*lang=json,strict*/
      """
      {
        "$type": "blackboard",
        "$v": 1,
        "values": {
          "serializable_blackboard_test_object": {
            "$type": "serializable_blackboard_test_object"
          }
        }
      }
      """;

    Should.Throw<JsonException>(
      () => JsonSerializer.Deserialize<SerializableBlackboard>(json, options)
    );
  }

  [Fact]
  public void DeserializationThrowsIfNullValue() {
    var options = new JsonSerializerOptions {
      WriteIndented = true,
      TypeInfoResolver = new SerializableTypeResolver(),
      Converters = { new SerializableTypeConverter(new Blackboard()) }
    };

    var json = /*lang=json,strict*/
      """
      {
        "$type": "blackboard",
        "$v": 1,
        "values": {
          "serializable_blackboard_test_object": null
        }
      }
      """;

    Should.Throw<JsonException>(
      () => JsonSerializer.Deserialize<SerializableBlackboard>(json, options)
    );
  }

  [Fact]
  public void DeserializationFailsIfJsonSerializerDeserializeReturnsNull() {
    var options = new JsonSerializerOptions {
      WriteIndented = true,
      TypeInfoResolver = new SerializableTypeResolver(),
      Converters = { new SerializableTypeConverter(new Blackboard()) }
    };

    var json = /*lang=json,strict*/"";

    Should.Throw<JsonException>(
      () => JsonSerializer.Deserialize<SerializableBlackboard>(json, options)
    );
  }
}
