namespace Chickensoft.Serialization.Tests;

using System.Text.Json;
using Chickensoft.Introspection;
using Chickensoft.Serialization.Tests.Fixtures;
using Shouldly;
using Xunit;

[Collection("NoRaceConditionWithStaticConverters")]
public partial class SerializableTypeResolverTest {
  [Meta, Id("type_resolver_test_model")]
  public partial record TypeResolverTestModel;

  [Fact]
  public void ReturnsACustomConverter() {

    var options = new JsonSerializerOptions { };

    var resolver = new SerializableTypeResolver();
    Serializer.AddConverter(new MyConverter());

    resolver.GetTypeInfo(typeof(string), options).ShouldNotBeNull();

    Serializer.RemoveConverter<MyConverter>();
  }
}
