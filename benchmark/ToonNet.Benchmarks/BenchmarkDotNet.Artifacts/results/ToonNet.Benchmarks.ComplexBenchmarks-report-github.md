```

BenchmarkDotNet v0.15.0, macOS 26.2 (25C56) [Darwin 25.2.0]
Apple M3 Max, 1 CPU, 16 logical and 16 physical cores
.NET SDK 10.0.100
  [Host]     : .NET 8.0.11 (8.0.1124.51707), Arm64 RyuJIT AdvSIMD
  Job-NVXBZW : .NET 8.0.11 (8.0.1124.51707), Arm64 RyuJIT AdvSIMD

WarmupCount=3  

```
| Method                | Mean       | Error    | StdDev   | Gen0   | Gen1   | Allocated |
|---------------------- |-----------:|---------:|---------:|-------:|-------:|----------:|
| SerializeGenerated    |   273.8 ns |  5.28 ns |  6.08 ns | 0.1807 | 0.0005 |   1.48 KB |
| SerializeReflection   | 2,755.1 ns | 18.47 ns | 17.28 ns | 0.5836 |      - |   4.78 KB |
| DeserializeGenerated  |   828.6 ns |  3.88 ns |  3.63 ns | 0.2270 |      - |   1.86 KB |
| DeserializeReflection | 6,519.7 ns | 37.19 ns | 32.97 ns | 1.8387 | 0.0229 |  15.07 KB |
