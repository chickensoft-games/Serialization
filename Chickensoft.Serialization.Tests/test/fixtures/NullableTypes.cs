namespace Chickensoft.Serialization.Tests.Fixtures;

using System.Collections.Generic;
using System.Text.Json.Serialization;
using Chickensoft.Introspection;

public readonly record struct MyValueType(string Name, int? OptionalInt);

[JsonSerializable(typeof(MyValueType))]
[JsonSerializable(typeof(MyValueType?))]
public partial class MyValueTypeContext : JsonSerializerContext;


[Meta, Id("nullable_value_types")]
public partial class NullableValueTypes {
  [Save("nullable_int")]
  public int? NullableInt { get; set; }

  [Save("nullable_bool")]
  public bool? NullableBool { get; set; }

  [Save("nullable_string")]
  public string? NullableString { get; set; }

  [Save("nullable_value")]
  public MyValueType? NullableValue { get; set; }

  [Save("nullable_value_list")]
  public List<MyValueType?>? NullableValueList { get; set; }

  [Save("nullable_int_list")]
  public List<int?>? NullableIntList { get; set; }
}
