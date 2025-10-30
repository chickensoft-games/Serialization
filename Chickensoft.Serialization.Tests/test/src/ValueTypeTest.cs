namespace Chickensoft.Serialization.Tests;

using System.Text.Json;
using System.Text.Json.Serialization;
using Chickensoft.Serialization.Tests.Fixtures;
using DeepEqual.Syntax;
using Shouldly;
using Xunit;

public class ValueTypeTest
{
  private readonly JsonSerializerOptions _options = new()
  {
    WriteIndented = true,
    TypeInfoResolver = new SerializableTypeResolver(),
    Converters = {
      new JsonStringEnumConverter<PetType>(),
      new SerializableTypeConverter()
    },
  };

  private readonly PersonValue _personWithDog = new()
  {
    Name = "John Doe",
    Age = 42,
    Pet = new DogValue
    {
      Name = "Fluffy",
      BarkVolume = 10
    },
  };

  private readonly string _serialized = /*lang=json,strict*/ """
    {
      "$type": "family_value",
      "$v": 1,
      "members": [
        {
          "$type": "person_value",
          "$v": 1,
          "age": 42,
          "name": "John Doe",
          "pet": {
            "$type": "dog_value",
            "$v": 1,
            "bark_volume": 10,
            "name": "Fluffy"
          }
        },
        {
          "$type": "person_value",
          "$v": 1,
          "age": 44,
          "name": "Jane Doe",
          "pet": {
            "$type": "cat_value",
            "$v": 1,
            "name": "Socks",
            "purr_volume": 5
          }
        }
      ]
    }
    """;

  private readonly PersonValue _personWithCat = new()
  {
    Name = "Jane Doe",
    Age = 44,
    Pet = new CatValue
    {
      Name = "Socks",
      PurrVolume = 5
    },
  };

  [Fact]
  public void Deserializes()
  {
    var family = JsonSerializer.Deserialize<FamilyValue>(
      _serialized, _options
    );

    family.Members.Count.ShouldBe(2);
    family.Members[0].ShouldDeepEqual(_personWithDog);
    family.Members[1].ShouldDeepEqual(_personWithCat);
  }

  [Fact]
  public void Serializes()
  {
    var family = new FamilyValue { Members = [_personWithDog, _personWithCat] };
    var data = JsonSerializer.Serialize(family, _options);

    data.ShouldBe(_serialized, StringCompareShould.IgnoreLineEndings);
  }

  [Fact]
  public void RoundTrip()
  {
    var family = new FamilyValue { Members = [_personWithDog, _personWithCat] };
    var data = JsonSerializer.Serialize(family, _options);

    var familyAgain = JsonSerializer.Deserialize<FamilyValue>(
      data, _options
    );

    familyAgain.ShouldDeepEqual(family);
  }
}
