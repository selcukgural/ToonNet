#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Models](ToonNet.Core.Models.md 'ToonNet\.Core\.Models')

## ToonArray Class

Represents an array of values in TOON format\.

```csharp
public sealed class ToonArray : ToonNet.Core.Models.ToonValue
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') &#129106; [ToonValue](ToonNet.Core.Models.ToonValue.md 'ToonNet\.Core\.Models\.ToonValue') &#129106; ToonArray

| Constructors | |
| :--- | :--- |
| [ToonArray\(\)](ToonNet.Core.Models.ToonArray..ctor.md#ToonNet.Core.Models.ToonArray.ToonArray() 'ToonNet\.Core\.Models\.ToonArray\.ToonArray\(\)') | Creates a new empty ToonArray\. |
| [ToonArray\(List&lt;ToonValue&gt;, string\[\]\)](ToonNet.Core.Models.ToonArray..ctor.md#ToonNet.Core.Models.ToonArray.ToonArray(System.Collections.Generic.List_ToonNet.Core.Models.ToonValue_,string[]) 'ToonNet\.Core\.Models\.ToonArray\.ToonArray\(System\.Collections\.Generic\.List\<ToonNet\.Core\.Models\.ToonValue\>, string\[\]\)') | Represents an array of values in TOON format\. |

| Properties | |
| :--- | :--- |
| [Count](ToonNet.Core.Models.ToonArray.Count.md 'ToonNet\.Core\.Models\.ToonArray\.Count') | Gets the number of items in this array\. |
| [FieldNames](ToonNet.Core.Models.ToonArray.FieldNames.md 'ToonNet\.Core\.Models\.ToonArray\.FieldNames') | Gets the optional field names for tabular arrays\. |
| [IsTabular](ToonNet.Core.Models.ToonArray.IsTabular.md 'ToonNet\.Core\.Models\.ToonArray\.IsTabular') | Gets a value indicating whether this is a tabular array \(array of objects with named fields\)\. |
| [Items](ToonNet.Core.Models.ToonArray.Items.md 'ToonNet\.Core\.Models\.ToonArray\.Items') | Gets the array items\. |
| [this\[int\]](ToonNet.Core.Models.ToonArray.this[int].md 'ToonNet\.Core\.Models\.ToonArray\.this\[int\]') | Gets or sets an array item by index\. |
| [ValueType](ToonNet.Core.Models.ToonArray.ValueType.md 'ToonNet\.Core\.Models\.ToonArray\.ValueType') | Gets the type of this value\. |

| Methods | |
| :--- | :--- |
| [Add\(ToonValue\)](ToonNet.Core.Models.ToonArray.Add(ToonNet.Core.Models.ToonValue).md 'ToonNet\.Core\.Models\.ToonArray\.Add\(ToonNet\.Core\.Models\.ToonValue\)') | Adds a value to the end of the array\. |
