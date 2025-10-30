namespace Chickensoft.Serialization.Tests;

using System.Text.Json;
using Chickensoft.Collections;
using Chickensoft.Introspection;
using Shouldly;
using Xunit;

public partial class InterfaceTest
{
  public interface IAliasedModel
  {
    int Value { get; }
  }

  [Meta, Id("aliased_model")]
  public partial class AliasedModel : IAliasedModel
  {
    [Save("value")]
    public int Value { get; set; }
  }

  [Meta, Id("interface_test_model")]
  public partial class TestModel
  {
    [Save("aliased_model")]
    public required IAliasedModel AliasedModel { get; set; }
  }

  [Fact]
  public void SerializesAsInterface()
  {
    var model = new TestModel
    {
      AliasedModel = new AliasedModel { Value = 10 }
    };

    var options = new JsonSerializerOptions
    {
      WriteIndented = true,
      TypeInfoResolver = new SerializableTypeResolver(),
      Converters = { new SerializableTypeConverter(new Blackboard()) }
    };

    var serialized = JsonSerializer.Serialize(model, options);

    serialized.ShouldBe(
      /*lang=json,strict*/
      """
      {
        "$type": "interface_test_model",
        "$v": 1,
        "aliased_model": {
          "$type": "aliased_model",
          "$v": 1,
          "value": 10
        }
      }
      """,
      StringCompareShould.IgnoreLineEndings
    );

    var deserialized = JsonSerializer.Deserialize<TestModel>(
      serialized, options
    );

    deserialized.ShouldNotBeNull();
    deserialized.AliasedModel.ShouldBeEquivalentTo(model.AliasedModel);
  }
}
