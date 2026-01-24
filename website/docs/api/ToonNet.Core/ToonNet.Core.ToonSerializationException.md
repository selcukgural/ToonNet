#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core](ToonNet.Core.md 'ToonNet\.Core')

## ToonSerializationException Class

Exception thrown during TOON serialization/deserialization\.

```csharp
public sealed class ToonSerializationException : ToonNet.Core.ToonException
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') &#129106; [System\.Exception](https://learn.microsoft.com/en-us/dotnet/api/system.exception 'System\.Exception') &#129106; [ToonException](ToonNet.Core.ToonException.md 'ToonNet\.Core\.ToonException') &#129106; ToonSerializationException

| Constructors | |
| :--- | :--- |
| [ToonSerializationException\(string\)](ToonNet.Core.ToonSerializationException..ctor.md#ToonNet.Core.ToonSerializationException.ToonSerializationException(string) 'ToonNet\.Core\.ToonSerializationException\.ToonSerializationException\(string\)') | Initializes a new instance of the ToonSerializationException class\. |
| [ToonSerializationException\(string, Exception\)](ToonNet.Core.ToonSerializationException..ctor.md#ToonNet.Core.ToonSerializationException.ToonSerializationException(string,System.Exception) 'ToonNet\.Core\.ToonSerializationException\.ToonSerializationException\(string, System\.Exception\)') | Initializes a new instance of the ToonSerializationException class with an inner exception\. |

| Properties | |
| :--- | :--- |
| [PropertyName](ToonNet.Core.ToonSerializationException.PropertyName.md 'ToonNet\.Core\.ToonSerializationException\.PropertyName') | Gets the property name where the error occurred\. |
| [TargetType](ToonNet.Core.ToonSerializationException.TargetType.md 'ToonNet\.Core\.ToonSerializationException\.TargetType') | Gets the target type being serialized or deserialized\. |
| [Value](ToonNet.Core.ToonSerializationException.Value.md 'ToonNet\.Core\.ToonSerializationException\.Value') | Gets the value that caused the error\. |

| Methods | |
| :--- | :--- |
| [Create\(string, Type, string, object, string\)](ToonNet.Core.ToonSerializationException.Create(string,System.Type,string,object,string).md 'ToonNet\.Core\.ToonSerializationException\.Create\(string, System\.Type, string, object, string\)') | Creates a serialization exception with context\. |
