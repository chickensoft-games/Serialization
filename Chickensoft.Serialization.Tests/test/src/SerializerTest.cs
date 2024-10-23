namespace Chickensoft.Serialization.Tests;

using System;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Chickensoft.Serialization.Tests.Fixtures;
using Shouldly;
using Xunit;

public partial class SerializationTest {
  private readonly JsonSerializerOptions _options = new() {
    WriteIndented = true,
    TypeInfoResolver = JsonTypeInfoResolver.Combine(
    new SerializableTypeResolver()
  ),
    Converters = {
      new SerializableTypeConverter()
    },
  };

  [Fact]
  public void ExpandsConverter() {
    var converter = Serializer.ExpandConverter(
      typeof(string), new MyConverterFactory(), _options
    );

    converter.ShouldNotBeNull();
  }

  [Fact]
  public void ExpandConverterThrowsIfAnotherFactoryIsFound() {
    Should.Throw<InvalidOperationException>(() =>
      Serializer.ExpandConverter(
        typeof(string), new BadConverterFactory(), _options
      )
    );
  }

  [Fact]
  public void BuiltInConverterFactories() {
    foreach (
      var converterFactory in Serializer.BuiltInConverterFactories
    ) {
      var converter = converterFactory.Value(_options);
      converter.ShouldNotBeNull();
    }
  }
}
