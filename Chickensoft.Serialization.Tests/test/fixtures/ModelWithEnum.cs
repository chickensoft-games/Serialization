namespace Chickensoft.Serialization.Tests.Fixtures;

using System.Text.Json.Serialization;
using Chickensoft.Introspection;

[JsonSerializable(typeof(ModelWithEnum.ModelType))]
public partial class ModelWithEnumContext : JsonSerializerContext;

[Meta, Id("model_with_enum")]
public partial record ModelWithEnum {
  [Save("a_type")]
  public ModelType AType { get; init; } = ModelType.Basic;

  [Save("b_type")]
  public ModelType BType { get; init; } = ModelType.Advanced;

  [Save("c_type")]
  public ModelType CType { get; init; }

  public enum ModelType {
    Basic,
    Advanced,
    Complex
  }
}
