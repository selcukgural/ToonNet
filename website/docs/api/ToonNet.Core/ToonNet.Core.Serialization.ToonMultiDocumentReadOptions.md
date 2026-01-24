#### [ToonNet\.Core](index.md 'index')
### [ToonNet\.Core\.Serialization](ToonNet.Core.Serialization.md 'ToonNet\.Core\.Serialization')

## ToonMultiDocumentReadOptions Class

Options for deserializing multiple TOON documents from a single stream\.

```csharp
public sealed class ToonMultiDocumentReadOptions
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') &#129106; ToonMultiDocumentReadOptions

| Properties | |
| :--- | :--- |
| [BlankLine](ToonNet.Core.Serialization.ToonMultiDocumentReadOptions.BlankLine.md 'ToonNet\.Core\.Serialization\.ToonMultiDocumentReadOptions\.BlankLine') | Gets a shared instance configured for blank\-line separation\. |
| [DocumentSeparator](ToonNet.Core.Serialization.ToonMultiDocumentReadOptions.DocumentSeparator.md 'ToonNet\.Core\.Serialization\.ToonMultiDocumentReadOptions\.DocumentSeparator') | Gets or sets the separator line used when [Mode](ToonNet.Core.Serialization.ToonMultiDocumentReadOptions.Mode.md 'ToonNet\.Core\.Serialization\.ToonMultiDocumentReadOptions\.Mode') is [ExplicitSeparator](ToonNet.Core.Serialization.ToonMultiDocumentSeparatorMode.md#ToonNet.Core.Serialization.ToonMultiDocumentSeparatorMode.ExplicitSeparator 'ToonNet\.Core\.Serialization\.ToonMultiDocumentSeparatorMode\.ExplicitSeparator')\. |
| [ExplicitSeparator](ToonNet.Core.Serialization.ToonMultiDocumentReadOptions.ExplicitSeparator.md 'ToonNet\.Core\.Serialization\.ToonMultiDocumentReadOptions\.ExplicitSeparator') | Gets a shared instance configured for explicit separator \(`---`\) separation\. |
| [Mode](ToonNet.Core.Serialization.ToonMultiDocumentReadOptions.Mode.md 'ToonNet\.Core\.Serialization\.ToonMultiDocumentReadOptions\.Mode') | Gets or sets the multi\-document separation mode\. |
