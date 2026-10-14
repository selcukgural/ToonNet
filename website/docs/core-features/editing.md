# Editing Trees

`ToonDocument.Parse` gives a tree of `ToonValue`s (`ToonObject`, `ToonArray`, `ToonString`, `ToonNumber`,
`ToonBoolean`, `ToonNull`) that can be read, changed and written back, in the manner of `System.Text.Json.Nodes`.

## Navigating

Every value has indexers, so lookups chain:

```csharp
using ToonNet.Core.Models;

var root = ToonDocument.Parse(toon).Root;

string name = root["users"]![0]["name"]!.GetValue<string>();
int total   = root["meta"]!["total"]!.GetValue<int>();
ToonValue? missing = root["nope"];                    // null: no such member
```

- `value["key"]` on an object returns the member or null; `value[i]` on an array returns the item. On a value of another
  kind they throw `InvalidOperationException` ("The value is an array, not an object.").
- `AsObject()` and `AsArray()` return the value as a `ToonObject` or `ToonArray`, or throw.
- `GetValue<T>()` converts a primitive to a string, boolean, number, date/time, `Guid`, `Uri` or enum with the serializer's
  rules (`"2025-03-04"` to `DateOnly`, `"Friday"` to `DayOfWeek`); it throws `ToonSerializationException` when the value
  does not convert. `TryGetValue<T>(out value)` returns false instead.
- `Deserialize<T>(options)` converts any value with the serializer (also with a `ToonTypeInfo`):
  `var users = root["users"]!.Deserialize<List<User>>();`

## Changing

```csharp
root["meta"]!["total"] = 3;                // set a member (null stores a TOON null)
root["users"]![0] = "gone";                // set an item
root["users"]!.AsArray().Add(new ToonObject { ["name"] = "Cy" });
root.AsObject().Properties.Remove("draft");
```

Paths set and remove values anywhere in the tree (see [Querying](querying) for the syntax):

```csharp
root.SetValue("$.meta.updatedBy", "selcuk");        // creates missing objects on the way
root.SetValue("$.audit.history[0].at", 1);          // an index equal to the length appends
int removed = root.RemoveAll("$.users[?@.active == false]");
```

`SetValue` takes a path of member names and indexes; a missing member becomes an object, or an array when an index
follows it. `RemoveAll` takes any path and removes what it selects (but not the root).

## Copying and Comparing

```csharp
var copy = root.DeepClone();                  // changes to the copy do not reach root
bool same = ToonValue.DeepEquals(root, copy); // true
```

`DeepEquals` compares data: objects with the same members in any order, arrays item by item, numbers by value (`1`
equals `1.0`). Primitives cannot change, so `DeepClone` shares them.

## Merge Patch

`ToonMergePatch` applies a JSON Merge Patch (RFC 7386): a document that looks like the target, with the members to
change, `null` for the members to remove, and the rest left out.

```csharp
var patch = ToonDocument.Parse("""
    meta:
      region: us
      draft: null
    """).Root;

root = ToonMergePatch.Apply(root, patch);   // sets meta.region, removes meta.draft
```

`Apply` changes objects in place and returns the result (a new value when the patch is not an object); call
`DeepClone` first to keep the original. An object that is in the tree twice (the same instance added at two places)
changes at both. `ToonMergePatch.Create(original, modified)` gives the patch between two trees,
so a client can send only what it changed:

```csharp
var patch = ToonMergePatch.Create(before, after);
string body = new ToonEncoder().Encode(new ToonDocument(patch));
```

A merge patch replaces arrays as a whole and cannot set a member to `null` (null removes it).

## See Also

- [Querying](querying): JSONPath over trees and text
- [Type System](type-system): how .NET types map to TOON values
