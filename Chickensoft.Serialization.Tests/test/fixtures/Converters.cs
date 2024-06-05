namespace Chickensoft.Serialization.Tests.Fixtures;

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

public class MyConverter : JsonConverter<string> {
  public override string? Read(
    ref Utf8JsonReader reader,
    Type typeToConvert,
    JsonSerializerOptions options
  ) => reader.GetString();

  public override void Write(
    Utf8JsonWriter writer,
    string value,
    JsonSerializerOptions options
  ) => writer.WriteStringValue(value);
}

public class MyConverterFactory : JsonConverterFactory {
  public override bool CanConvert(Type typeToConvert) =>
    typeToConvert == typeof(string);

  public override JsonConverter CreateConverter(
    Type typeToConvert,
    JsonSerializerOptions options
  ) => new MyConverter();
}

public class BadConverterFactory : JsonConverterFactory {
  public override bool CanConvert(Type typeToConvert) =>
    typeToConvert == typeof(string);

  public override JsonConverter CreateConverter(
    Type typeToConvert,
    JsonSerializerOptions options
  ) => new MyConverterFactory();
}
