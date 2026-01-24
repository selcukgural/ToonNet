#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Models](ToonNet.Core.Models.md 'ToonNet\.Core\.Models').[ToonArray](ToonNet.Core.Models.ToonArray.md 'ToonNet\.Core\.Models\.ToonArray')

## ToonArray Constructors

| Overloads | |
| :--- | :--- |
| [ToonArray\(\)](ToonNet.Core.Models.ToonArray..ctor.md#ToonNet.Core.Models.ToonArray.ToonArray() 'ToonNet\.Core\.Models\.ToonArray\.ToonArray\(\)') | Creates a new empty ToonArray\. |
| [ToonArray\(List&lt;ToonValue&gt;, string\[\]\)](ToonNet.Core.Models.ToonArray..ctor.md#ToonNet.Core.Models.ToonArray.ToonArray(System.Collections.Generic.List_ToonNet.Core.Models.ToonValue_,string[]) 'ToonNet\.Core\.Models\.ToonArray\.ToonArray\(System\.Collections\.Generic\.List\<ToonNet\.Core\.Models\.ToonValue\>, string\[\]\)') | Represents an array of values in TOON format\. |

<a name='ctor.md#ToonNet.Core.Models.ToonArray.ToonArray()'></a>

## ToonArray\(\) Constructor

Creates a new empty ToonArray\.

```csharp
public ToonArray();
```

<a name='ctor.md#ToonNet.Core.Models.ToonArray.ToonArray(System.Collections.Generic.List_ToonNet.Core.Models.ToonValue_,string[])'></a>

## ToonArray\(List\<ToonValue\>, string\[\]\) Constructor

Represents an array of values in TOON format\.

```csharp
public ToonArray(System.Collections.Generic.List<ToonNet.Core.Models.ToonValue> items, string[]? fieldNames=null);
```
#### Parameters

<a name='ToonNet.Core.Models.ToonArray.ToonArray(System.Collections.Generic.List_ToonNet.Core.Models.ToonValue_,string[]).items'></a>

`items` [System\.Collections\.Generic\.List&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.list-1 'System\.Collections\.Generic\.List\`1')[ToonValue](ToonNet.Core.Models.ToonValue.md 'ToonNet\.Core\.Models\.ToonValue')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.list-1 'System\.Collections\.Generic\.List\`1')

The list of items to initialize the array with\.

<a name='ToonNet.Core.Models.ToonArray.ToonArray(System.Collections.Generic.List_ToonNet.Core.Models.ToonValue_,string[]).fieldNames'></a>

`fieldNames` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')[\[\]](https://learn.microsoft.com/en-us/dotnet/api/system.array 'System\.Array')

Optional field names for tabular arrays\.