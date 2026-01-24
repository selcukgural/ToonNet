#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Models](ToonNet.Core.Models.md 'ToonNet\.Core\.Models').[ToonObject](ToonNet.Core.Models.ToonObject.md 'ToonNet\.Core\.Models\.ToonObject')

## ToonObject Constructors

| Overloads | |
| :--- | :--- |
| [ToonObject\(\)](ToonNet.Core.Models.ToonObject..ctor.md#ToonNet.Core.Models.ToonObject.ToonObject() 'ToonNet\.Core\.Models\.ToonObject\.ToonObject\(\)') | Creates a new empty ToonObject\. |
| [ToonObject\(Dictionary&lt;string,ToonValue&gt;\)](ToonNet.Core.Models.ToonObject..ctor.md#ToonNet.Core.Models.ToonObject.ToonObject(System.Collections.Generic.Dictionary_string,ToonNet.Core.Models.ToonValue_) 'ToonNet\.Core\.Models\.ToonObject\.ToonObject\(System\.Collections\.Generic\.Dictionary\<string,ToonNet\.Core\.Models\.ToonValue\>\)') | Represents an object \(key\-value pairs\) in TOON format\. |

<a name='ctor.md#ToonNet.Core.Models.ToonObject.ToonObject()'></a>

## ToonObject\(\) Constructor

Creates a new empty ToonObject\.

```csharp
public ToonObject();
```

<a name='ctor.md#ToonNet.Core.Models.ToonObject.ToonObject(System.Collections.Generic.Dictionary_string,ToonNet.Core.Models.ToonValue_)'></a>

## ToonObject\(Dictionary\<string,ToonValue\>\) Constructor

Represents an object \(key\-value pairs\) in TOON format\.

```csharp
public ToonObject(System.Collections.Generic.Dictionary<string,ToonNet.Core.Models.ToonValue> properties);
```
#### Parameters

<a name='ToonNet.Core.Models.ToonObject.ToonObject(System.Collections.Generic.Dictionary_string,ToonNet.Core.Models.ToonValue_).properties'></a>

`properties` [System\.Collections\.Generic\.Dictionary&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.dictionary-2 'System\.Collections\.Generic\.Dictionary\`2')[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')[,](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.dictionary-2 'System\.Collections\.Generic\.Dictionary\`2')[ToonValue](ToonNet.Core.Models.ToonValue.md 'ToonNet\.Core\.Models\.ToonValue')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.dictionary-2 'System\.Collections\.Generic\.Dictionary\`2')

The dictionary of properties to initialize the object with\.