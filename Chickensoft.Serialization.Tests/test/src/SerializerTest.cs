namespace Chickensoft.Serialization.Tests;

using System;
using System.Text.Json;
using Chickensoft.Serialization.Tests.Fixtures;
using Shouldly;
using Xunit;

public partial class SerializationTest {
  [Fact]
  public void ExpandsConverter() {
    var options = new JsonSerializerOptions { };

    var converter = Serializer.ExpandConverter(
      typeof(string), new MyConverterFactory(), options
    );

    converter.ShouldNotBeNull();
  }

  [Fact]
  public void ExpandConverterThrowsIfAnotherFactoryIsFound() {
    var options = new JsonSerializerOptions { };

    Should.Throw<InvalidOperationException>(() =>
      Serializer.ExpandConverter(
        typeof(string), new BadConverterFactory(), options
      )
    );
  }

  [Fact]
  public void BuiltInConverterFactories() {
    var options = new JsonSerializerOptions { };
    foreach (
      var converterFactory in Serializer.BuiltInConverterFactories.Values
    ) {
      converterFactory(options).ShouldNotBeNull();
    }
  }
}
