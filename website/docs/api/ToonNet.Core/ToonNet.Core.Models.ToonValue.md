#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Models](ToonNet.Core.Models.md 'ToonNet\.Core\.Models')

## ToonValue Class

Represents a value in TOON format\.

```csharp
public abstract class ToonValue
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') &#129106; ToonValue

Derived  
&#8627; [ToonArray](ToonNet.Core.Models.ToonArray.md 'ToonNet\.Core\.Models\.ToonArray')  
&#8627; [ToonBoolean](ToonNet.Core.Models.ToonBoolean.md 'ToonNet\.Core\.Models\.ToonBoolean')  
&#8627; [ToonNull](ToonNet.Core.Models.ToonNull.md 'ToonNet\.Core\.Models\.ToonNull')  
&#8627; [ToonNumber](ToonNet.Core.Models.ToonNumber.md 'ToonNet\.Core\.Models\.ToonNumber')  
&#8627; [ToonObject](ToonNet.Core.Models.ToonObject.md 'ToonNet\.Core\.Models\.ToonObject')  
&#8627; [ToonString](ToonNet.Core.Models.ToonString.md 'ToonNet\.Core\.Models\.ToonString')

### Remarks
This is the base class for all TOON value types, such as null, boolean, number, string, object, and array\.
It provides a common interface for accessing the type of the value\.

| Properties | |
| :--- | :--- |
| [ValueType](ToonNet.Core.Models.ToonValue.ValueType.md 'ToonNet\.Core\.Models\.ToonValue\.ValueType') | Gets the type of this value\. |

| Operators | |
| :--- | :--- |
| [implicit operator ToonValue\(bool\)](ToonNet.Core.Models.ToonValue.op_ImplicitToonNet.Core.Models.ToonValue(bool).md 'ToonNet\.Core\.Models\.ToonValue\.op\_Implicit ToonNet\.Core\.Models\.ToonValue\(bool\)') | Implicitly converts a boolean value to a ToonValue\. |
| [implicit operator ToonValue\(decimal\)](ToonNet.Core.Models.ToonValue.op_ImplicitToonNet.Core.Models.ToonValue(decimal).md 'ToonNet\.Core\.Models\.ToonValue\.op\_Implicit ToonNet\.Core\.Models\.ToonValue\(decimal\)') | Implicitly converts a decimal value to a ToonValue\. |
| [implicit operator ToonValue\(double\)](ToonNet.Core.Models.ToonValue.op_ImplicitToonNet.Core.Models.ToonValue(double).md 'ToonNet\.Core\.Models\.ToonValue\.op\_Implicit ToonNet\.Core\.Models\.ToonValue\(double\)') | Implicitly converts a double value to a ToonValue\. |
| [implicit operator ToonValue\(float\)](ToonNet.Core.Models.ToonValue.op_ImplicitToonNet.Core.Models.ToonValue(float).md 'ToonNet\.Core\.Models\.ToonValue\.op\_Implicit ToonNet\.Core\.Models\.ToonValue\(float\)') | Implicitly converts a float value to a ToonValue\. |
| [implicit operator ToonValue\(int\)](ToonNet.Core.Models.ToonValue.op_ImplicitToonNet.Core.Models.ToonValue(int).md 'ToonNet\.Core\.Models\.ToonValue\.op\_Implicit ToonNet\.Core\.Models\.ToonValue\(int\)') | Implicitly converts an integer value to a ToonValue\. |
| [implicit operator ToonValue\(long\)](ToonNet.Core.Models.ToonValue.op_ImplicitToonNet.Core.Models.ToonValue(long).md 'ToonNet\.Core\.Models\.ToonValue\.op\_Implicit ToonNet\.Core\.Models\.ToonValue\(long\)') | Implicitly converts a long value to a ToonValue\. |
| [implicit operator ToonValue\(string\)](ToonNet.Core.Models.ToonValue.op_ImplicitToonNet.Core.Models.ToonValue(string).md 'ToonNet\.Core\.Models\.ToonValue\.op\_Implicit ToonNet\.Core\.Models\.ToonValue\(string\)') | Implicitly converts a string value to a ToonValue\. |
