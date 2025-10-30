namespace Chickensoft.Serialization.Tests;

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
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
public class ModelWithEnumTest
{
  [Fact]
  public void DeserializesAndRespectsDefaultValues()
  {
    var value = /*lang=json,strict*/ """
    {
      "$type": "model_with_enum",
      "$v": 1,
      "c_type": "Complex"
    }
    """;

    var options = new JsonSerializerOptions
    {
      WriteIndented = true,
      TypeInfoResolver = JsonTypeInfoResolver.Combine(
        ModelWithEnumContext.Default,
        new SerializableTypeResolver()
      ),
      Converters = {
        new JsonStringEnumConverter<ModelWithEnum.ModelType>(),
        new SerializableTypeConverter()
      },
    };

    var result = JsonSerializer.Deserialize<ModelWithEnum>(value, options);

    result.ShouldNotBeNull();

    result.AType.ShouldBe(ModelWithEnum.ModelType.Basic); // default value
    result.BType.ShouldBe(ModelWithEnum.ModelType.Advanced); // default value

    result.CType.ShouldBe(ModelWithEnum.ModelType.Complex); // from JSON
  }

  [Fact]
  public void DeserializesAndOverridesDefaultValues()
  {
    var value = /*lang=json,strict*/ """
    {
      "$type": "model_with_enum",
      "$v": 1,
      "a_type": "Advanced",
      "b_type": "Basic"
    }
    """;

    var options = new JsonSerializerOptions
    {
      WriteIndented = true,
      TypeInfoResolver = JsonTypeInfoResolver.Combine(
        ModelWithEnumContext.Default,
        new SerializableTypeResolver()
      ),
      Converters = {
        new JsonStringEnumConverter<ModelWithEnum.ModelType>(),
        new SerializableTypeConverter()
      },
    };

    var result = JsonSerializer.Deserialize<ModelWithEnum>(value, options);

    result.ShouldNotBeNull();

    result.AType.ShouldBe(ModelWithEnum.ModelType.Advanced); // default value
    result.BType.ShouldBe(ModelWithEnum.ModelType.Basic); // default value

    // missing json values should result in whatever is the 0 value of the enum
    result.CType.ShouldBe(ModelWithEnum.ModelType.Basic);
  }
}
