namespace Chickensoft.Serialization.Tests;

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Chickensoft.Collections;
using Chickensoft.Introspection;
using DeepEqual.Syntax;
using Shouldly;
using Xunit;

[
  SuppressMessage(
    "Performance",
    "CA1869",
    Justification = "We want new JsonSerializerOptions for each test"
  )
]
[Collection("NoRaceConditionWithStaticConverters")]
public partial class CollectionsTest
{
  [Meta, Id("book")]
  public partial record Book
  {
    [Save("title")]
    public required string Title { get; set; }

    [Save("author")]
    public required string Author { get; set; }

    [Save("related_books")]
    public Dictionary<string, List<HashSet<string>>>? RelatedBooks { get; set; }
  }

  [Meta, Id("bookcase")]
  public partial record Bookcase
  {
    [Save("books")]
    public required List<Book> Books { get; set; }
  }

  [Fact]
  public void SimpleSerializationCase()
  {
    var book = new Book
    {
      Title = "The Book",
      Author = "The Author",
      RelatedBooks = new Dictionary<string, List<HashSet<string>>>
      {
        ["Title A"] = [["Author A", "Author B"]],
      }
    };

    var options = new JsonSerializerOptions
    {
      WriteIndented = true,
      TypeInfoResolver = new SerializableTypeResolver(),
      Converters = { new SerializableTypeConverter(new Blackboard()) }
    };

    var json = JsonSerializer.Serialize(book, options);

    json.ShouldBe(
      /*lang=json,strict*/
      """
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
      """,
      options: StringCompareShould.IgnoreLineEndings
    );
  }

  [Fact]
  public void SerializesCollections()
  {
    var book = new Book
    {
      Title = "The Book",
      Author = "The Author",
      RelatedBooks = new Dictionary<string, List<HashSet<string>>>
      {
        ["Title A"] = [
          ["Author A", "Author B"],
          ["Author C", "Author D"],
        ],
        ["Title B"] = [
          ["Author E", "Author F"],
          ["Author G", "Author H"],
          []
        ],
        ["Title C"] = []
      },
    };

    var options = new JsonSerializerOptions
    {
      WriteIndented = true,
      TypeInfoResolver = new SerializableTypeResolver(),
      Converters = { new SerializableTypeConverter(new Blackboard()) }
    };

    var json = JsonSerializer.Serialize(book, options);

    json.ShouldBe(
      /*lang=json,strict*/
      """
      {
        "$type": "book",
        "$v": 1,
        "author": "The Author",
        "related_books": {
          "Title A": [
            [
              "Author A",
              "Author B"
            ],
            [
              "Author C",
              "Author D"
            ]
          ],
          "Title B": [
            [
              "Author E",
              "Author F"
            ],
            [
              "Author G",
              "Author H"
            ],
            []
          ],
          "Title C": []
        },
        "title": "The Book"
      }
      """,
      options: StringCompareShould.IgnoreLineEndings
    );

    var bookAgain = JsonSerializer.Deserialize<Book>(json, options);

    bookAgain.ShouldNotBeNull();
    bookAgain.ShouldDeepEqual(book);
  }

  [Fact]
  public void DeserializesMissingCollectionsToEmptyOnes()
  {
    var options = new JsonSerializerOptions
    {
      TypeInfoResolver = new SerializableTypeResolver(),
      Converters = { new SerializableTypeConverter(new Blackboard()) },
      WriteIndented = true
    };

    var json =
      /*lang=json,strict*/
      """
      {
        "$type": "book",
        "$v": 1,
        "author": "The Author",
        "title": "The Book"
      }
      """;

    var book = JsonSerializer.Deserialize<Book>(json, options)!;

    book.RelatedBooks.ShouldBeEmpty();
  }


  [Fact]
  public void DeserializesExplicitlyNullCollectionsToNull()
  {
    var options = new JsonSerializerOptions
    {
      TypeInfoResolver = new SerializableTypeResolver(),
      Converters = { new SerializableTypeConverter(new Blackboard()) },
      WriteIndented = true
    };

    var json =
      /*lang=json,strict*/
      """
      {
        "$type": "book",
        "$v": 1,
        "author": "The Author",
        "related_books": null,
        "title": "The Book"
      }
      """;

    var book = JsonSerializer.Deserialize<Book>(json, options)!;

    book.RelatedBooks.ShouldBeNull();
  }

  [Fact]
  public void SerializesAList()
  {
    var bookcase = new Bookcase
    {
      Books = [
        new() { Title = "Title A", Author = "Author A" },
        new() { Title = "Title B", Author = "Author B" },
        new() { Title = "Title C", Author = "Author C" }
      ]
    };

    var options = new JsonSerializerOptions
    {
      TypeInfoResolver = new SerializableTypeResolver(),
      Converters = { new SerializableTypeConverter(new Blackboard()) },
      WriteIndented = true
    };

    var json = JsonSerializer.Serialize(bookcase, options);

    json.ShouldBe(
      /*lang=json,strict*/
      """
      {
        "$type": "bookcase",
        "$v": 1,
        "books": [
          {
            "$type": "book",
            "$v": 1,
            "author": "Author A",
            "related_books": null,
            "title": "Title A"
          },
          {
            "$type": "book",
            "$v": 1,
            "author": "Author B",
            "related_books": null,
            "title": "Title B"
          },
          {
            "$type": "book",
            "$v": 1,
            "author": "Author C",
            "related_books": null,
            "title": "Title C"
          }
        ]
      }
      """,
      options: StringCompareShould.IgnoreLineEndings
    );

    var bookcaseAgain = JsonSerializer.Deserialize<Bookcase>(json, options);

    bookcaseAgain.ShouldNotBeNull();
    bookcaseAgain.ShouldDeepEqual(bookcase);
  }
}
