namespace Chickensoft.Serialization.Tests.Fixtures;

using System.Collections.Generic;
using Chickensoft.Introspection;


[Meta, Id("family_value")]
public readonly partial record struct FamilyValue {
  [Save("members")]
  public List<PersonValue> Members { get; init; }
}

[Meta, Id("person_value")]
public readonly partial record struct PersonValue {
  [Save("name")]
  public string Name { get; init; }

  [Save("age")]
  public int Age { get; init; }

  [Save("pet")]
  public IPet Pet { get; init; }
}

public interface IPet {
  string Name { get; init; }

  PetType Type { get; }
}

[Meta, Id("dog_value")]
public readonly partial record struct DogValue : IPet {
  [Save("name")]
  public required string Name { get; init; }

  [Save("bark_volume")]
  public required int BarkVolume { get; init; }

  public PetType Type => PetType.Dog;

  public DogValue() { }
}

[Meta, Id("cat_value")]
public readonly partial record struct CatValue : IPet {
  [Save("name")]
  public required string Name { get; init; }

  [Save("purr_volume")]
  public required int PurrVolume { get; init; }

  public PetType Type => PetType.Cat;

  public CatValue() { }
}
