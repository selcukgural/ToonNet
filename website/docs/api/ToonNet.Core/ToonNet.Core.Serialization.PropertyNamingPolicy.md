#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Serialization](ToonNet.Core.Serialization.md 'ToonNet\.Core\.Serialization')

## PropertyNamingPolicy Enum

Property naming policies\.

```csharp
public enum PropertyNamingPolicy
```
### Fields

<a name='ToonNet.Core.Serialization.PropertyNamingPolicy.Default'></a>

`Default` 0

Use property names as\-is \(PascalCase in C\#\)\.

<a name='ToonNet.Core.Serialization.PropertyNamingPolicy.CamelCase'></a>

`CamelCase` 1

Convert to camelCase \(e\.g\., firstName\)\.

<a name='ToonNet.Core.Serialization.PropertyNamingPolicy.SnakeCase'></a>

`SnakeCase` 2

Convert to snake\_case \(e\.g\., first\_name\)\.

<a name='ToonNet.Core.Serialization.PropertyNamingPolicy.LowerCase'></a>

`LowerCase` 3

Convert to lowercase\.