#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Serialization](ToonNet.Core.Serialization.md 'ToonNet\.Core\.Serialization').[ToonSerializer](ToonNet.Core.Serialization.ToonSerializer.md 'ToonNet\.Core\.Serialization\.ToonSerializer')

## ToonSerializer\.Serialize Method

| Overloads | |
| :--- | :--- |
| [Serialize\(object, Type, ToonSerializerOptions\)](ToonNet.Core.Serialization.ToonSerializer.Serialize.md#ToonNet.Core.Serialization.ToonSerializer.Serialize(object,System.Type,ToonNet.Core.Serialization.ToonSerializerOptions) 'ToonNet\.Core\.Serialization\.ToonSerializer\.Serialize\(object, System\.Type, ToonNet\.Core\.Serialization\.ToonSerializerOptions\)') | Serializes an object to TOON format string \(non\-generic overload\)\. |
| [Serialize&lt;T&gt;\(T, ToonSerializerOptions\)](ToonNet.Core.Serialization.ToonSerializer.Serialize.md#ToonNet.Core.Serialization.ToonSerializer.Serialize_T_(T,ToonNet.Core.Serialization.ToonSerializerOptions) 'ToonNet\.Core\.Serialization\.ToonSerializer\.Serialize\<T\>\(T, ToonNet\.Core\.Serialization\.ToonSerializerOptions\)') | Serializes an object to TOON format string\. |

<a name='ToonNet.Core.Serialization.ToonSerializer.Serialize(object,System.Type,ToonNet.Core.Serialization.ToonSerializerOptions)'></a>

## ToonSerializer\.Serialize\(object, Type, ToonSerializerOptions\) Method

Serializes an object to TOON format string \(non\-generic overload\)\.

```csharp
public static string Serialize(object? value, System.Type type, ToonNet.Core.Serialization.ToonSerializerOptions? options=null);
```
#### Parameters

<a name='ToonNet.Core.Serialization.ToonSerializer.Serialize(object,System.Type,ToonNet.Core.Serialization.ToonSerializerOptions).value'></a>

`value` [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object')

The value to serialize\.

<a name='ToonNet.Core.Serialization.ToonSerializer.Serialize(object,System.Type,ToonNet.Core.Serialization.ToonSerializerOptions).type'></a>

`type` [System\.Type](https://learn.microsoft.com/en-us/dotnet/api/system.type 'System\.Type')

The type of object to serialize\.

<a name='ToonNet.Core.Serialization.ToonSerializer.Serialize(object,System.Type,ToonNet.Core.Serialization.ToonSerializerOptions).options'></a>

`options` [ToonSerializerOptions](ToonNet.Core.Serialization.ToonSerializerOptions.md 'ToonNet\.Core\.Serialization\.ToonSerializerOptions')

Optional serialization options\.

#### Returns
[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')  
The TOON format string representation of the object\.

#### Exceptions

[System\.ArgumentNullException](https://learn.microsoft.com/en-us/dotnet/api/system.argumentnullexception 'System\.ArgumentNullException')  
Thrown when type is null\.

[ToonEncodingException](ToonNet.Core.ToonEncodingException.md 'ToonNet\.Core\.ToonEncodingException')  
Thrown when serialization fails\.

<a name='ToonNet.Core.Serialization.ToonSerializer.Serialize_T_(T,ToonNet.Core.Serialization.ToonSerializerOptions)'></a>

## ToonSerializer\.Serialize\<T\>\(T, ToonSerializerOptions\) Method

Serializes an object to TOON format string\.

```csharp
public static string Serialize{T}(T? value, ToonNet.Core.Serialization.ToonSerializerOptions? options=null);
```
#### Type parameters

<a name='ToonNet.Core.Serialization.ToonSerializer.Serialize_T_(T,ToonNet.Core.Serialization.ToonSerializerOptions).T'></a>

`T`

The type of object to serialize\.
#### Parameters

<a name='ToonNet.Core.Serialization.ToonSerializer.Serialize_T_(T,ToonNet.Core.Serialization.ToonSerializerOptions).value'></a>

`value` [T](ToonNet.Core.Serialization.ToonSerializer.md#ToonNet.Core.Serialization.ToonSerializer.Serialize_T_(T,ToonNet.Core.Serialization.ToonSerializerOptions).T 'ToonNet\.Core\.Serialization\.ToonSerializer\.Serialize\<T\>\(T, ToonNet\.Core\.Serialization\.ToonSerializerOptions\)\.T')

The value to serialize\.

<a name='ToonNet.Core.Serialization.ToonSerializer.Serialize_T_(T,ToonNet.Core.Serialization.ToonSerializerOptions).options'></a>

`options` [ToonSerializerOptions](ToonNet.Core.Serialization.ToonSerializerOptions.md 'ToonNet\.Core\.Serialization\.ToonSerializerOptions')

Optional serialization options\.

#### Returns
[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')  
The TOON format string representation of the object\.

#### Exceptions

[ToonEncodingException](ToonNet.Core.ToonEncodingException.md 'ToonNet\.Core\.ToonEncodingException')  
Thrown when serialization fails\.