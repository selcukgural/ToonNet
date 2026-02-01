```

BenchmarkDotNet v0.15.0, macOS 26.2 (25C56) [Darwin 25.2.0]
Apple M3 Max, 1 CPU, 16 logical and 16 physical cores
.NET SDK 10.0.100
  [Host]     : .NET 8.0.11 (8.0.1124.51707), Arm64 RyuJIT AdvSIMD
  Job-NZRPBE : .NET 8.0.11 (8.0.1124.51707), Arm64 RyuJIT AdvSIMD

WarmupCount=3  

```
| Method                | Mean       | Error    | StdDev   | Gen0   | Gen1   | Allocated |
|---------------------- |-----------:|---------:|---------:|-------:|-------:|----------:|
| SerializeGenerated    |   267.7 ns |  4.73 ns |  4.43 ns | 0.1807 | 0.0005 |   1.48 KB |
| SerializeReflection   | 2,443.5 ns | 32.93 ns | 27.49 ns | 0.4921 |      - |   4.04 KB |
| DeserializeGenerated  |   824.1 ns |  3.93 ns |  3.68 ns | 0.2270 |      - |   1.86 KB |
| DeserializeReflection | 6,324.1 ns | 74.95 ns | 66.44 ns | 1.7471 | 0.0153 |  14.33 KB |
