```

BenchmarkDotNet v0.15.0, macOS 26.2 (25C56) [Darwin 25.2.0]
Apple M3 Max, 1 CPU, 16 logical and 16 physical cores
.NET SDK 10.0.100
  [Host]     : .NET 8.0.11 (8.0.1124.51707), Arm64 RyuJIT AdvSIMD
  Job-RPWNZH : .NET 8.0.11 (8.0.1124.51707), Arm64 RyuJIT AdvSIMD

WarmupCount=3  

```
| Method                | Mean       | Error    | StdDev   | Gen0   | Gen1   | Allocated |
|---------------------- |-----------:|---------:|---------:|-------:|-------:|----------:|
| SerializeGenerated    |   264.8 ns |  2.32 ns |  1.81 ns | 0.1807 | 0.0005 |   1.48 KB |
| SerializeReflection   | 2,389.9 ns | 20.08 ns | 17.80 ns | 0.4921 |      - |   4.04 KB |
| DeserializeGenerated  |   831.5 ns | 16.22 ns | 19.31 ns | 0.2270 |      - |   1.86 KB |
| DeserializeReflection | 6,294.5 ns | 66.43 ns | 55.47 ns | 1.7471 | 0.0153 |  14.33 KB |
