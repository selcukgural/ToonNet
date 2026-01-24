#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core](ToonNet.Core.md 'ToonNet\.Core').[ToonException](ToonNet.Core.ToonException.md 'ToonNet\.Core\.ToonException')

## ToonException Constructors

| Overloads | |
| :--- | :--- |
| [ToonException\(string\)](ToonNet.Core.ToonException..ctor.md#ToonNet.Core.ToonException.ToonException(string) 'ToonNet\.Core\.ToonException\.ToonException\(string\)') | Initializes a new instance of the ToonException class with a specified error message\. |
| [ToonException\(string, Exception\)](ToonNet.Core.ToonException..ctor.md#ToonNet.Core.ToonException.ToonException(string,System.Exception) 'ToonNet\.Core\.ToonException\.ToonException\(string, System\.Exception\)') | Initializes a new instance of the ToonException class with a specified error message and a reference to the inner exception\. |

<a name='ctor.md#ToonNet.Core.ToonException.ToonException(string)'></a>

## ToonException\(string\) Constructor

Initializes a new instance of the ToonException class with a specified error message\.

```csharp
public ToonException(string message);
```
#### Parameters

<a name='ToonNet.Core.ToonException.ToonException(string).message'></a>

`message` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

The message that describes the error\.

<a name='ctor.md#ToonNet.Core.ToonException.ToonException(string,System.Exception)'></a>

## ToonException\(string, Exception\) Constructor

Initializes a new instance of the ToonException class with a specified error message and a reference to the inner exception\.

```csharp
public ToonException(string message, System.Exception innerException);
```
#### Parameters

<a name='ToonNet.Core.ToonException.ToonException(string,System.Exception).message'></a>

`message` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

The error message that explains the reason for the exception\.

<a name='ToonNet.Core.ToonException.ToonException(string,System.Exception).innerException'></a>

`innerException` [System\.Exception](https://learn.microsoft.com/en-us/dotnet/api/system.exception 'System\.Exception')

The exception that is the cause of the current exception\.