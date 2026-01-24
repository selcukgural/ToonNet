#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Serialization](ToonNet.Core.Serialization.md 'ToonNet\.Core\.Serialization').[ToonSerializer](ToonNet.Core.Serialization.ToonSerializer.md 'ToonNet\.Core\.Serialization\.ToonSerializer')

## ToonSerializer\.GetTypeMetadata\(Type, bool\) Method

Retrieves cached type metadata for a specified type, or generates and stores it if not already cached\.

```csharp
private static ToonNet.Core.Serialization.ToonSerializer.TypeMetadata GetTypeMetadata(System.Type type, bool includeReadOnly);
```
#### Parameters

<a name='ToonNet.Core.Serialization.ToonSerializer.GetTypeMetadata(System.Type,bool).type'></a>

`type` [System\.Type](https://learn.microsoft.com/en-us/dotnet/api/system.type 'System\.Type')

The type for which metadata is retrieved or created\.

<a name='ToonNet.Core.Serialization.ToonSerializer.GetTypeMetadata(System.Type,bool).includeReadOnly'></a>

`includeReadOnly` [System\.Boolean](https://learn.microsoft.com/en-us/dotnet/api/system.boolean 'System\.Boolean')

Specifies whether to include read\-only properties in the metadata\.

#### Returns
[TypeMetadata](ToonNet.Core.Serialization.ToonSerializer.TypeMetadata.md 'ToonNet\.Core\.Serialization\.ToonSerializer\.TypeMetadata')  
An instance of `TypeMetadata` containing metadata for the specified type\.