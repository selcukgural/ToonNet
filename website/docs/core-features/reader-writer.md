# Reader and Writer

`ToonDocumentWriter` and `ToonDocumentReader` write and read TOON documents forward-only, value by value, without
building a `ToonDocument` tree. Use them when the data does not come from (or go to) .NET objects: rows from a database
reader, a document you transform on the fly, or a large file you scan for a few values.

## Writing

TOON headers carry the length of an array (`items[3]:`), so each array is started with the number of items, rows or
entries it will have; the writer checks the count when the array ends.

```csharp
using ToonNet.Core.Encoding;

using var stream = File.Create("sale.toon");
using var writer = new ToonDocumentWriter(stream);   // also IBufferWriter<byte> (UTF-8) or TextWriter, and ToonOptions

writer.WriteStartObject();
writer.WriteString("name", "Spring sale");
writer.WriteNumber("count", 3);

writer.WriteStartArray("tags", 2);                    // inline primitives
writer.WriteStringValue("new");
writer.WriteStringValue("sale");
writer.WriteEndArray();

writer.WriteStartTable("users", 2, ["id", "name"]);   // one value per field and row
writer.WriteNumberValue(1);
writer.WriteStringValue("Ada");
writer.WriteNumberValue(2);
writer.WriteStringValue("Bob");
writer.WriteEndArray();

writer.WriteStartKeyedTable("servers", 2, ["host", "port"]);
writer.WriteRowKey("eu");
writer.WriteStringValue("eu.example.com");
writer.WriteNumberValue(443);
writer.WriteRowKey("us");
writer.WriteStringValue("us.example.com");
writer.WriteNumberValue(8443);
writer.WriteEndArray();

writer.WriteStartList("items", 2);                    // expanded list: any items
writer.WriteStartObject();
writer.WriteString("kind", "box");
writer.WriteEndObject();
writer.WriteNullValue();
writer.WriteEndArray();

writer.WriteEndObject();
```

```toon
name: Spring sale
count: 3
tags[2]: new,sale
users[2]{id,name}:
  1,Ada
  2,Bob
servers[2:]{host,port}:
  eu: eu.example.com,443
  us: us.example.com,8443
items[2]:
  - kind: box
  - null
```

- The layout is `ToonEncoder`'s: the same structure gives the same text, with the options' indentation and delimiter.
- A value inside an object needs a property name; inline arrays, table rows and keyed rows take primitives only; a table
  cannot be a list item (write a list of objects, as the encoder does). Misuse throws `InvalidOperationException`.
- `WriteValue(ToonValue)` and `WriteValue(name, ToonValue)` write a tree where a value goes, in the form the encoder picks
  for it (including tables with nested field groups).
- The text is collected in a pooled buffer and written by `Flush`, `FlushAsync` and `Dispose`. Flush every few hundred
  rows to keep memory flat while writing a large table to a stream.
- The writer does not track property names: writing one twice in an object gives a document that strict readers reject
  (a repeated key).

| Writer members | |
|---------|---|
| `WriteStartObject()`, `WriteStartObject(name)`, `WriteEndObject()` | Objects; the overloads without a name are for the root, list items and values |
| `WriteStartArray`, `WriteStartList`, `WriteStartTable`, `WriteStartKeyedTable` (each with or without a name, with a length and the fields), `WriteRowKey(key)`, `WriteEndArray()` | Arrays and tables |
| `WriteStringValue`, `WriteNumberValue` (`long`, `ulong`, `decimal`, `double`, `float`), `WriteBooleanValue`, `WriteNullValue` | A value where a value goes (an item, a cell) |
| `WriteString(name, ...)`, `WriteNumber(name, ...)`, `WriteBoolean(name, bool)`, `WriteNull(name)` | A member of an object; strings also as `ReadOnlySpan<char>` |
| `WriteValue(value)`, `WriteValue(name, value)` | A `ToonValue` tree |
| `Flush()`, `FlushAsync(ct)`, `Dispose()`, `DisposeAsync()` | Write the collected text (disposing flushes) |
| `Options`, `CurrentDepth`, `IsComplete` | The options it formats with, the objects and arrays not yet ended, whether the root value is complete |

## Reading

The reader yields tokens that follow the data model, not the layout: tabular rows, keyed entries and list items are
objects and arrays like any other.

```csharp
using ToonNet.Core.Models;
using ToonNet.Core.Parsing;

byte[] utf8Bytes = File.ReadAllBytes("sale.toon");
using var reader = new ToonDocumentReader(utf8Bytes);   // also string, ReadOnlySpan<char>, Create(stream), CreateAsync(stream)

while (reader.Read())
{
    switch (reader.TokenType)
    {
        case ToonTokenType.PropertyName when reader.GetPropertyName() == "users":
            reader.Read();                               // StartArray
            Console.WriteLine($"{reader.ArrayLength} users");
            break;
        case ToonTokenType.PropertyName when reader.GetPropertyName() == "servers":
            ToonValue servers = reader.ReadValue();      // the whole value as a tree
            break;
        case ToonTokenType.PropertyName when reader.GetPropertyName() == "items":
            reader.Skip();                               // not needed
            break;
    }
}
```

| Token | Getters |
|-------|---------|
| `StartObject`, `EndObject` | |
| `StartArray`, `EndArray` | `ArrayLength` (declared length) on `StartArray` |
| `PropertyName` | `GetPropertyName()` |
| `String` | `GetString()` (unescaped) |
| `Number` | `GetInt32()`, `GetInt64()`, `GetDecimal()`, `GetDouble()`; `TryGetInt32`, `TryGetInt64`, `TryGetDecimal` return false when the number does not fit (the `Get` methods throw `FormatException`) |
| `True`, `False` | `GetBoolean()` |
| `Null` | |

A getter for another token type throws `InvalidOperationException`. `ValueSpan` is the raw text of a primitive token
(a quoted string with its quotes and escapes), `LineNumber` its line, and `CurrentDepth` the number of objects and
arrays entered and not yet ended. `Skip()` skips the current value (on a property name, its value) and `ReadValue()` reads it as a tree; both leave the reader on the value's last token, so the next `Read()` moves past it. The reader checks the document with the
rules and errors of `ToonDocument.Parse` (a `ToonParseException` with line and column), and the end of the document when
`Read` returns false. In non-strict mode a repeated key is reported each time it appears, in document order; a tree
(`ReadValue`, `ToonDocument.Parse`) keeps the last value. UTF-8 input is decoded into a pooled buffer; dispose the reader
to return it (the getters throw `ObjectDisposedException` afterwards).

### Values Into Objects

`ToonSerializer.Deserialize<T>(reader)` converts the value the reader is on, straight from the text, as
`JsonSerializer` reads from a `Utf8JsonReader`. A large document can be read partly by hand and partly into objects:

```csharp
using ToonNet.Core.Parsing;
using ToonNet.Core.Serialization;

using var reader = new ToonDocumentReader(utf8Bytes);

while (reader.Read())
{
    if (reader.TokenType == ToonTokenType.PropertyName && reader.GetPropertyName() == "orders")
    {
        reader.Read();                                    // StartArray

        while (reader.Read() && reader.TokenType != ToonTokenType.EndArray)
        {
            var order = ToonSerializer.Deserialize<Order>(reader);   // one row at a time
            Process(order);
        }
    }
}
```

The reader is on a property name (its value is read), the start of a value, or a primitive; afterwards it is on the
value's last token, as after `ReadValue`. A reader that has not read yet starts at the root. Results and errors are those
of `Deserialize<T>(string)`; after an error, the reader cannot be used. The reader's own options decide how the text is
parsed. With a `ToonTypeInfo`, it reads without reflection.

## See Also

- [Querying](querying): `DeserializeAt` and `DeserializeAll` read values at a path without a loop like this one
- [Serialization](serialization) and [Deserialization](deserialization): .NET objects to and from TOON
- [Streaming](streaming): multi-document files
