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
      NullableValue = new MyValueType("My Value", null),
      NullableValueList = [
        new MyValueType("Value 1", 1),
        new MyValueType("Value 2", 2),
      ],
      NullableIntList = [1, null, 3],
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
        "nullable_int_list": [
          1,
          null,
          3
        ],
        "nullable_string": "a",
        "nullable_value": {
          "Name": "My Value",
          "OptionalInt": null
        },
        "nullable_value_list": [
          {
            "Name": "Value 1",
            "OptionalInt": 1
          },
          {
            "Name": "Value 2",
            "OptionalInt": 2
          }
        ]
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
    var model = new NullableValueTypes() {
      NullableValueList = [null, new MyValueType("Value 2", 2)],
      NullableIntList = [1, null, 2],
    };

    var json = JsonSerializer.Serialize(model, _options);

    json.ShouldBe(
      /*lang=json,strict*/
      """
      {
        "$type": "nullable_value_types",
        "$v": 1,
        "nullable_bool": null,
        "nullable_int": null,
        "nullable_int_list": [
          1,
          null,
          2
        ],
        "nullable_string": null,
        "nullable_value": null,
        "nullable_value_list": [
          null,
          {
            "Name": "Value 2",
            "OptionalInt": 2
          }
        ]
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
    deserialized.NullableValueList.ShouldNotBeNull();
    deserialized.NullableValueList!.Count.ShouldBe(2);
    deserialized.NullableValueList[0].ShouldBeNull();
    deserialized.NullableValueList[1].ShouldNotBeNull();
    deserialized.NullableValueList[1]!.Value.Name.ShouldBe("Value 2");
    deserialized.NullableValueList[1]!.Value.OptionalInt.ShouldBe(2);
  }
}
