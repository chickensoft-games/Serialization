namespace Chickensoft.Serialization.Tests.Fixtures;

using System.Text.Json.Serialization;
using Chickensoft.Introspection;

[JsonSerializable(typeof(MyEnum))]
public partial class MyAppContext : JsonSerializerContext;

public enum MyEnum {
  One, Two, Three
}

[Meta, Id("my_model_with_an_enum")]
public partial record MyModelWithAnEnum {
  [Save("value")]
  public MyEnum Value { get; set; }
}

