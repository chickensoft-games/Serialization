namespace Chickensoft.Serialization.Tests;

using System.Text.Json;
using System.Text.Json.Serialization;
using Chickensoft.Collections;
using Chickensoft.Serialization.Tests.Fixtures;
using DeepEqual.Syntax;
using Shouldly;
using Xunit;

public class MyEnumAotTest {
  [Fact]
  public void CanUseEnumsWithStjGeneratedMetadataForUseInAotEnvironments() {
    var options = new JsonSerializerOptions {
      Converters = {
        new JsonStringEnumConverter<MyEnum>(),
        new SerializableTypeConverter(new Blackboard())
      },
      WriteIndented = true,
      TypeInfoResolverChain = {
        // If mixing and matching, always provide the chickensoft type
        // resolver as the first resolver in the chain :)
        new SerializableTypeResolver(),
        // Generated serialization contexts:
        MyAppContext.Default
      }
    };

    var model = new MyModelWithAnEnum { Value = MyEnum.Two };
    var serialized = JsonSerializer.Serialize(model, options);

    var deserialized = JsonSerializer.Deserialize<MyModelWithAnEnum>(
      serialized, options
    );

    deserialized.ShouldBeOfType<MyModelWithAnEnum>().ShouldDeepEqual(model);
  }
}
