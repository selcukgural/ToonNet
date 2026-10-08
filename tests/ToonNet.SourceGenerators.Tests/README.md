# ToonNet.SourceGenerators.Tests

Tests for [`ToonNet.SourceGenerators`](../../src/ToonNet.SourceGenerators). They run on .NET 8 and .NET 10.

## Running

```bash
dotnet test tests/ToonNet.SourceGenerators.Tests
```

## Layout

```
ToonNet.SourceGenerators.Tests/
├── Models/                              # [ToonSerializable] types; the generator runs on them at build time
│   ├── SimpleModel.cs                   # primitives, nullables, fixed naming policies
│   ├── Phase4Models.cs                  # nested objects, [ToonConverter], [ToonConstructor]
│   ├── ModernModels.cs                  # records, structs, init/required, inheritance, generics, collections
│   └── OtherNamespaceModels.cs          # another namespace and the global namespace
└── Tests/
    ├── SerializationGeneratorTests.cs   # round trips of the simple models
    ├── Phase4FeatureTests.cs            # nesting, converters, constructors
    ├── ReflectionParityTests.cs         # generated output == ToonSerializer output, for every model and option
    └── GeneratorDriverTests.cs          # runs the generator on source snippets: diagnostics, unusual shapes compile
```

`ReflectionParityTests` is the main safety net: for each model it checks that the generated `Serialize` encodes to
exactly what `ToonSerializer.Serialize` produces, and that the generated `Deserialize` reads it back to an equal value.

## Viewing generated code

```bash
dotnet build tests/ToonNet.SourceGenerators.Tests -f net10.0 -p:EmitCompilerGeneratedFiles=true
ls tests/ToonNet.SourceGenerators.Tests/obj/Debug/net10.0/generated/ToonNet.SourceGenerators/
```
