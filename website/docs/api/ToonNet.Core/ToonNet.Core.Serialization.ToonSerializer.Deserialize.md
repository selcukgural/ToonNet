#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Serialization](ToonNet.Core.Serialization.md 'ToonNet\.Core\.Serialization').[ToonSerializer](ToonNet.Core.Serialization.ToonSerializer.md 'ToonNet\.Core\.Serialization\.ToonSerializer')

## ToonSerializer\.Deserialize Method

| Overloads | |
| :--- | :--- |
| [Deserialize\(string, Type, ToonSerializerOptions\)](ToonNet.Core.Serialization.ToonSerializer.Deserialize.md#ToonNet.Core.Serialization.ToonSerializer.Deserialize(string,System.Type,ToonNet.Core.Serialization.ToonSerializerOptions) 'ToonNet\.Core\.Serialization\.ToonSerializer\.Deserialize\(string, System\.Type, ToonNet\.Core\.Serialization\.ToonSerializerOptions\)') | Deserializes a TOON format string to an object of the specified type\. |
| [Deserialize&lt;T&gt;\(string, ToonSerializerOptions\)](ToonNet.Core.Serialization.ToonSerializer.Deserialize.md#ToonNet.Core.Serialization.ToonSerializer.Deserialize_T_(string,ToonNet.Core.Serialization.ToonSerializerOptions) 'ToonNet\.Core\.Serialization\.ToonSerializer\.Deserialize\<T\>\(string, ToonNet\.Core\.Serialization\.ToonSerializerOptions\)') | Deserializes a TOON format string to an object\. |

<a name='ToonNet.Core.Serialization.ToonSerializer.Deserialize(string,System.Type,ToonNet.Core.Serialization.ToonSerializerOptions)'></a>

## ToonSerializer\.Deserialize\(string, Type, ToonSerializerOptions\) Method

Deserializes a TOON format string to an object of the specified type\.

```csharp
public static object? Deserialize(string toonString, System.Type type, ToonNet.Core.Serialization.ToonSerializerOptions? options=null);
```
#### Parameters

<a name='ToonNet.Core.Serialization.ToonSerializer.Deserialize(string,System.Type,ToonNet.Core.Serialization.ToonSerializerOptions).toonString'></a>

`toonString` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

The TOON format string to deserialize\.

<a name='ToonNet.Core.Serialization.ToonSerializer.Deserialize(string,System.Type,ToonNet.Core.Serialization.ToonSerializerOptions).type'></a>

`type` [System\.Type](https://learn.microsoft.com/en-us/dotnet/api/system.type 'System\.Type')

The target type to deserialize to\.

<a name='ToonNet.Core.Serialization.ToonSerializer.Deserialize(string,System.Type,ToonNet.Core.Serialization.ToonSerializerOptions).options'></a>

`options` [ToonSerializerOptions](ToonNet.Core.Serialization.ToonSerializerOptions.md 'ToonNet\.Core\.Serialization\.ToonSerializerOptions')

Optional deserialization options\.

#### Returns
[System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object')  
The deserialized object, or null if the input is null\.

#### Exceptions

[ToonParseException](ToonNet.Core.ToonParseException.md 'ToonNet\.Core\.ToonParseException')  
Thrown when parsing fails\.

<a name='ToonNet.Core.Serialization.ToonSerializer.Deserialize_T_(string,ToonNet.Core.Serialization.ToonSerializerOptions)'></a>

## ToonSerializer\.Deserialize\<T\>\(string, ToonSerializerOptions\) Method

Deserializes a TOON format string to an object\.

```csharp
public static T? Deserialize{T}(string toonString, ToonNet.Core.Serialization.ToonSerializerOptions? options=null);
```
#### Type parameters

<a name='ToonNet.Core.Serialization.ToonSerializer.Deserialize_T_(string,ToonNet.Core.Serialization.ToonSerializerOptions).T'></a>

`T`

The type to deserialize to\.
#### Parameters

<a name='ToonNet.Core.Serialization.ToonSerializer.Deserialize_T_(string,ToonNet.Core.Serialization.ToonSerializerOptions).toonString'></a>

`toonString` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

The TOON format string to deserialize\.

<a name='ToonNet.Core.Serialization.ToonSerializer.Deserialize_T_(string,ToonNet.Core.Serialization.ToonSerializerOptions).options'></a>

`options` [ToonSerializerOptions](ToonNet.Core.Serialization.ToonSerializerOptions.md 'ToonNet\.Core\.Serialization\.ToonSerializerOptions')

Optional deserialization options\.

#### Returns
[T](ToonNet.Core.Serialization.ToonSerializer.md#ToonNet.Core.Serialization.ToonSerializer.Deserialize_T_(string,ToonNet.Core.Serialization.ToonSerializerOptions).T 'ToonNet\.Core\.Serialization\.ToonSerializer\.Deserialize\<T\>\(string, ToonNet\.Core\.Serialization\.ToonSerializerOptions\)\.T')  
The deserialized object, or null if the input is null\.

#### Exceptions

[ToonParseException](ToonNet.Core.ToonParseException.md 'ToonNet\.Core\.ToonParseException')  
Thrown when parsing fails\.