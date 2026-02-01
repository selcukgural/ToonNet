```

BenchmarkDotNet v0.15.0, macOS 26.2 (25C56) [Darwin 25.2.0]
Apple M3 Max, 1 CPU, 16 logical and 16 physical cores
.NET SDK 10.0.100
  [Host]     : .NET 8.0.11 (8.0.1124.51707), Arm64 RyuJIT AdvSIMD
  Job-ZNQJPI : .NET 8.0.11 (8.0.1124.51707), Arm64 RyuJIT AdvSIMD

WarmupCount=3  

```
| Method                | Mean       | Error    | StdDev   | Gen0   | Allocated |
|---------------------- |-----------:|---------:|---------:|-------:|----------:|
| SerializeGenerated    |   114.5 ns |  2.21 ns |  1.96 ns | 0.0889 |     744 B |
| SerializeReflection   |   793.6 ns |  4.91 ns |  5.46 ns | 0.1917 |    1608 B |
| DeserializeGenerated  |   228.0 ns |  1.09 ns |  0.97 ns | 0.1118 |     936 B |
| DeserializeReflection | 2,164.4 ns | 40.45 ns | 37.83 ns | 0.7286 |    6120 B |
