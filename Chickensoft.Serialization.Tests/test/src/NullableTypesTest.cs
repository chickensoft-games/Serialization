namespace Chickensoft.Serialization.Tests;

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Chickensoft.Serialization.Tests.Fixtures;
using Shouldly;
using Xunit;

public class NullableValueTypesTest {
  private readonly JsonSerializerOptions _options = new() {
    WriteIndented = true,
    TypeInfoResolver = JsonTypeInfoResolver.Combine(
      MyValueTypeContext.Default,
      new SerializableTypeResolver()
    ),
    Converters = {
      new SerializableTypeConverter()
    },
  };

  [Fact]
  public void SerializationRoundTripWithValues() {
    var model = new NullableValueTypes {
      NullableBool = true,
      NullableInt = 42,
      NullableString = "a",
      NullableValue = new MyValueType("My Value", null)
    };

    var json = JsonSerializer.Serialize(model, _options);

    json.ShouldBe(
      /*lang=json,strict*/
      """
      {
        "$type": "nullable_value_types",
        "$v": 1,
        "nullable_bool": true,
        "nullable_int": 42,
        "nullable_string": "a",
        "nullable_value": {
          "Name": "My Value",
          "OptionalInt": null
        }
      }
      """,
      StringCompareShould.IgnoreLineEndings
    );

    var deserialized = JsonSerializer.Deserialize<NullableValueTypes>(json, _options);

    deserialized.ShouldNotBeNull();
    deserialized.NullableInt!.Value.ShouldBe(42);
    deserialized.NullableBool!.Value.ShouldBeTrue();
    deserialized.NullableString.ShouldBe("a");
    deserialized.NullableValue.ShouldNotBeNull();
    deserialized.NullableValue!.Value.Name.ShouldBe("My Value");
    deserialized.NullableValue!.Value.OptionalInt.ShouldBeNull();
  }

  [Fact]
  public void SerializationRoundTripWithoutValues() {
    var model = new NullableValueTypes();

    var json = JsonSerializer.Serialize(model, _options);

    json.ShouldBe(
      /*lang=json,strict*/
      """
      {
        "$type": "nullable_value_types",
        "$v": 1,
        "nullable_bool": null,
        "nullable_int": null,
        "nullable_string": null,
        "nullable_value": null
      }
      """,
      StringCompareShould.IgnoreLineEndings
    );

    var deserialized = JsonSerializer.Deserialize<NullableValueTypes>(json, _options);

    deserialized.ShouldNotBeNull();
    deserialized.NullableInt.HasValue.ShouldBeFalse();
    deserialized.NullableBool.HasValue.ShouldBeFalse();
    deserialized.NullableString.ShouldBeNull();
    deserialized.NullableValue.ShouldBeNull();
  }
}
