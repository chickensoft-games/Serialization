# 💾 Serialization

[![Chickensoft Badge][chickensoft-badge]][chickensoft-website] [![Discord][discord-badge]][discord] [![Read the docs][read-the-docs-badge]][docs] ![line coverage][line-coverage] ![branch coverage][branch-coverage]

System.Text.Json-compatible source generator with automatic support for derived types and polymorphic serialization.

---

<p align="center">
<img alt="Chickensoft.Serialization" src="Chickensoft.Serialization/icon.png" width="200">
</p>

## 📕 Background

- ✅ Support 0-configuration polymorphic serialization in AOT builds.
- ✅ Support versioning and upgrading outdated models.
- ✅ Allow types to access and customize their own JSON representation via serialization/deserialization hooks.
- ✅ Support abstract types.
- ✅ Support nested types.

The Chickensoft Serialization system allows you to easily declare serializable types that will work when compiled for ahead-of-time (AOT) environments, like iOS. It can be easily used alongside the `System.Text.Json` source generators for more complex usage scenarios.

```csharp
  [Meta, Id("book")]
  public partial record Book {
    [Save("title")]
    public required string Title { get; set; }

    [Save("author")]
    public required string Author { get; set; }

    [Save("related_books")]
    public Dictionary<string, List<HashSet<string>>>? RelatedBooks { get; set; }
  }

  [Meta, Id("bookcase")]
  public partial record Bookcase {
    [Save("books")]
    public required List<Book> Books { get; set; }
  }
```

Example model:

```csharp
var book = new Book {
  Title = "The Book",
  Author = "The Author",
  RelatedBooks = new Dictionary<string, List<HashSet<string>>> {
    ["Title A"] = [["Author A", "Author B"]],
  }
};
```

Serialized JSON:

```json
{
  "$type": "book",
  "$v": 1,
  "author": "The Author",
  "related_books": {
    "Title A": [
      [
        "Author A",
        "Author B"
      ]
    ]
  },
  "title": "The Book"
}
```

### 🥳 Overview

The serialization system is designed to be simple and easy to use. Under the hood, it leverages the Chickensoft [Introspection] generator to avoid using reflection that isn't supported when targeting AOT builds. The Chickensoft Introspection generator is also decently fast, since it only uses syntax nodes instead of relying on analyzer symbol data, which can be very slow.

The serialization system uses the same, somewhat obscure (but public) API's that the generated output of the `System.Text.Json` source generators use to define metadata about serializable types.

Annoyingly, `System.Text.Json` requires you to tag derived types on the generation context, which makes refactoring type hierarchies painful and prone to human error if you forget to update. The Chickensoft serialization system automatically handles derived types so you don't have to think about polymorphic serialization and maintain a list of types anywhere.

### ✋ Intentional Limitations

- ❌ Generic types are not supported.
- ❌ Models must have parameterless constructors.
- ❌ Serializable types must be partial.
- ❌ Only collections supported are `HashSet<T>`, `List<T>`, and `Dictionary<TKey, TValue>`.
- ❌ Referencing types by an interface is not supported.

The Chickensoft serializer has strong opinions about how JSON serialization should be done. It's primarily intended to simplify the process of defining models for game save files, but you can use it any C# project which supports C# >= 11.

> [!TIP]
> [Keep your JSON models simple][json-complexity].

If you must do something fancy, the serialization system integrates seamlessly with `System.Text.Json` and generated serializer contexts. The Chickensoft serialization system is essentially just a special `IJsonTypeInfoResolver` and `JsonConverter<object>` working together.

## 🥚 Installation

You'll need the serialization package, as well as the [Introspection] package and its source generator.

Make sure you get the latest versions of the packages here on nuget: [Chickensoft.Introspection], [Chickensoft.Introspection.Generator], [Chickensoft.Serialization].

```xml
<PackageReference Include="Chickensoft.Serialization" Version=... />
<PackageReference Include="Chickensoft.Introspection" Version=... />
<PackageReference Include="Chickensoft.Introspection.Generator" Version=... PrivateAssets="all" OutputItemType="analyzer" />
```

> [!WARNING]
> Don't forget the `PrivateAssets="all" OutputItemType="analyzer"` when including a source generator package in your project.

## 💾 Serializable Types

### 𝚫 Defining a Serializable Type

To declare a serializable model, add the `[Meta]` and `[Id]` attributes to a type.

> When your project is built, the `Introspection` generator will produce a registry of all the types visible from the global scope of your project, as well as varying levels of metadata about the types based on whether they are instantiable, introspective, versioned, and/or identifiable. For more information, check out the [Introspection] generator readme.

```csharp
using Chickensoft.Introspection;

[Meta, Id("model")]
public partial class Model { }
```

> [!CAUTION]
> Note that a model's `id` needs to be globally unique across all serializable types in every assembly that your project uses. The `id` is used as the model's [type discriminator] for polymorphic deserialization.

### 📼 Serializing and Deserializing

The serialization system leverages the serialization infrastructure provided by `System.Text.Json`. To use it, simply create a `JsonSerializerOptions` instance with a `SerializableTypeResolver` and `SerializableTypeConverter`.

```csharp
var options = new JsonSerializerOptions {
  WriteIndented = true,
  TypeInfoResolver = new SerializableTypeResolver(),
  Converters = { new SerializableTypeConverter() }
};

var model = new Model();

var json = JsonSerializer.Serialize(model, options);

var modelAgain = JsonSerializer.Deserialize<Model>(json, options);
```

### ☑️ Defining Serializable Properties

To define a serializable property, add the `[Save]` attribute to the property, specifying its json name.

```csharp
[Meta, Id("model")]
public partial class Model {
  [Save("name")]
  public required string Name { get; init; } // required allows it to be non-nullable

  [Save("description")]
  public string? Description { get; init; } // not required, should be nullable 
}
```

> [!TIP]
> By default, properties are not serialized. This omit-by-default policy enables you to inherit functionality from other types while adding support for serialization in scenarios where you do not fully control the type hierarchy.
>
> Fields are never serialized.

For best results, mark non-nullable properties as [`required`] and use `init` properties for models.

### 🪆 Polymorphism

Abstract types are supported. Serializable types inherit serializable properties from base types.

> [!TIP]
> Instead of placing an `[Id]` on the abstract type, place it on each derived type.

```csharp
[Meta]
public abstract partial class Person {
  [Save("name")]
  public required string Name { get; init; }
}

[Meta, Id("doctor")]
public partial class Doctor : Person {
  [Save("specialty")]
  public required string Specialty { get; init; }
}

[Meta, Id("lawyer")]
public partial class Lawyer : Person {
  [Save("cases_won")]
  public required int CasesWon { get; init; }  
}
```

> [!CAUTION]
> A serializable property cannot refer to a type by an interface.

## ⏳ Versioning

The serialization system provides support for versioning models when requirements inevitably change.

There are some situations where adding non-required fields to an existing model is not possible, such as when the type of a field changes or you want to introduce a required property.

Fortunately, the serialization system allows you to declare multiple versions of the same model. Version numbers are simple integer values.

### 👯‍♀️ Defining Multiple Versions of a Type

The following `LogEntry` model extends a non-serializable type `SystemLogEntry`. We will introduce a change to the `Type` property, making it a `LogType` enum instead of a string.

```csharp
[Meta, Id("log_entry")]
public abstract partial class LogEntry : SystemLogEntry {
  [Save("text")]
  public required string Text { get; init; }

  [Save("type")]
  public required string Type { get; init; }
}
```

To introduce a new version, you first need to create a common base type for all the versions.

We first rename the current `LogEntry` to `LogEntry1` and introduce a new abstract type which extends `SystemLogEntry` — a type that we don't have direct control over. Then, we simply update the `LogEntry1` model to inherit from the abstract `LogEntry`.

By default, instantiable introspective types have a default version of `1`. We will go ahead and add the `[Version]` attribute anyways to make it more clear.

```csharp
// We make an abstract type that the specific versions extend.
[Meta, Id("log_entry")]
public abstract partial class LogEntry : SystemLogEntry { }

// Used to be LogEntry, but is now LogEntry1.
[Meta, Version(1)]
public partial class LogEntry1 : LogEntry {
  [Save("text")]
  public required string Text { get; init; }

  [Save("type")]
  public required string Type { get; init; }
}
```

> [!TIP]
> Note that the `[Id]` attribute is only on the abstract base log entry type.

Finally, we can introduce a new version:

```csharp
public enum LogType {
  Info,
  Warning,
  Error
}

[Meta, Version(2)]
public partial class LogEntry2 : LogEntry {
  [Save("text")]
  public required string Text { get; init; }

  [Save("type")]
  public required LogType Type { get; init; }
}
```

### ✨ Never Out of Date

When deserializing older versions of models, the serialization system will automatically upgrade models that implement the `IOutdated` interface. The `IOutdated` interface requires that we implement an `Upgrade` method.

We can update the previous example by marking the first model as outdated:

```csharp
[Meta, Id("log_entry")]
public abstract partial class LogEntry { }

[Meta, Version(1)]
public partial class LogEntry1 : LogEntry, IOutdated {
  [Save("text")]
  public required string Text { get; init; }

  [Save("type")]
  public required string Type { get; init; }

  public object Upgrade(IReadOnlyBlackboard deps) => new LogEntry2() {
    Text = Text,
    Type = Type switch {
      "info" => LogType.Info,
      "warning" => LogType.Warning,
      "error" or _ => LogType.Error,
    }
  };
}
```

> [!TIP]
> Types will continue to be upgraded until a type that is not `IOutdated` is returned.

The upgrade method receives a [blackboard] which can be used to lookup dependencies the type might need to upgrade itself. When setting up the serialization system, you must provide the blackboard.

```csharp
// If our types need access to a service to upgrade themselves, we can
// set that up here when creating the serialization options.
var upgradeDependencies = new Blackboard();
upgradeDependencies.Set(new MyService());

var options = new JsonSerializerOptions {
  WriteIndented = true,
  TypeInfoResolver = new SerializableTypeResolver(),
  Converters = { new IdentifiableTypeConverter(new Blackboard()) }
};

var model = JsonSerializer.Deserialize<LogEntry>(json, options);
```

## 🪝 Serialization Hooks

Types can implement `ICustomSerializable` to customize how they are serialized and deserialized.

```csharp
  [Meta, Id("custom_serializable")]
  public partial class CustomSerializable : ICustomSerializable {
    public int Value { get; set; }

    public object OnDeserialized(
      IdentifiableTypeMetadata metadata,
      JsonObject json,
      JsonSerializerOptions options
    ) {
      Value = json["value"]?.GetValue<int>() ?? -1;

      return this;
    }

    public void OnSerialized(
      IdentifiableTypeMetadata metadata,
      JsonObject json,
      JsonSerializerOptions options
    ) {
      // Even though our property doesn't have the [Save] attribute, we
      // can save it manually.
      json["value"] = Value;
    }
  }
```

The `OnDeserialized` and `OnSerialized` methods each receive the type's generated introspection [metadata], the [`JsonObject`][JsonObject] node, and the `JsonSerializerOptions`.

Types can add, modify, or remove properties during `OnSerialized`. Likewise, `OnDeserialized` allows a type to read data directly from the Json nodes that it is being deserialized from.

## 💬 Registering Converters

You can inform the serializer about types which have custom converters.

```csharp
public class MyCustomJsonConverter : JsonConverter<T> { ... }

Serializer.AddConverter(new MyCustomJsonConverter());
```

Converters registered this way do not need to be specified in the `JsonSerializerOptions`, which allows other libraries to extend the serialization system without requiring additional effort from the developer using the library.

## 💌 Built-in Types

The serialization system has built-in support for a number of types. If a type is not on this list, you will have to make your own `JsonConverter<T>` for it and register it with the serialization system (or else you will get a runtime error during serialization/deserialization).

### 🫙 Collections

- `HashSet<T>`
- `List<T>`
- `Dictionary<TKey, TValue>`

### 🧰 Basic Types

- `bool`
- `byte[]`
- `byte`
- `char`
- `DateTime`
- `DateTimeOffset`
- `decimal`
- `double`
- `Guid`
- `short`
- `int`
- `long`
- `JsonArray`
- `JsonDocument`
- `JsonElement`
- `JsonNode`
- `JsonObject`
- `JsonValue`
- `Memory<byte>`
- `object`
- `ReadOnlyMemory<byte>`
- `sbyte`
- `float`
- `string`
- `TimeSpan`
- `ushort`
- `uint`
- `ulong`
- `Uri`
- `Version`

---

🐣 Package generated from a 🐤 Chickensoft Template — <https://chickensoft.games>

[chickensoft-badge]: https://raw.githubusercontent.com/chickensoft-games/chickensoft_site/main/static/img/badges/chickensoft_badge.svg
[chickensoft-website]: https://chickensoft.games
[discord-badge]: https://raw.githubusercontent.com/chickensoft-games/chickensoft_site/main/static/img/badges/discord_badge.svg
[discord]: https://discord.gg/gSjaPgMmYW
[read-the-docs-badge]: https://raw.githubusercontent.com/chickensoft-games/chickensoft_site/main/static/img/badges/read_the_docs_badge.svg
[docs]: https://chickensoft.games/docs
[line-coverage]: Chickensoft.Serialization.Tests/badges/line_coverage.svg
[branch-coverage]: Chickensoft.Serialization.Tests/badges/branch_coverage.svg

[Introspection]: https://github.com/chickensoft-games/Introspection
[json-complexity]: https://einarwh.wordpress.com/2020/05/08/on-the-complexity-of-json-serialization/
[Chickensoft.Introspection]: https://www.nuget.org/packages/Chickensoft.Introspection
[Chickensoft.Introspection.Generator]: https://www.nuget.org/packages/Chickensoft.Introspection.Generator
[Chickensoft.Serialization]: https://www.nuget.org/packages/Chickensoft.Serialization
[type discriminator]: https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/polymorphism?pivots=dotnet-7-0#polymorphic-type-discriminators
[blackboard]: https://github.com/chickensoft-games/Collections?tab=readme-ov-file#blackboard
[metadata]: https://github.com/chickensoft-games/Introspection?tab=readme-ov-file#-metadata-types
[JsonObject]: https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/use-dom#use-jsonnode
