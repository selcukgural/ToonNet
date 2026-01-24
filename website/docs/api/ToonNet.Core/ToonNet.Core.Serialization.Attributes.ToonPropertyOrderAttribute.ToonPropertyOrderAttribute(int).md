#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Serialization\.Attributes](ToonNet.Core.Serialization.Attributes.md 'ToonNet\.Core\.Serialization\.Attributes').[ToonPropertyOrderAttribute](ToonNet.Core.Serialization.Attributes.ToonPropertyOrderAttribute.md 'ToonNet\.Core\.Serialization\.Attributes\.ToonPropertyOrderAttribute')

## ToonPropertyOrderAttribute\(int\) Constructor

Specifies the order of properties during serialization\.

```csharp
public ToonPropertyOrderAttribute(int order);
```
#### Parameters

<a name='ToonNet.Core.Serialization.Attributes.ToonPropertyOrderAttribute.ToonPropertyOrderAttribute(int).order'></a>

`order` [System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')

The order value that determines the position of the property during serialization\.

### Remarks
This attribute can be applied to properties or fields to control their order in the serialized output\.