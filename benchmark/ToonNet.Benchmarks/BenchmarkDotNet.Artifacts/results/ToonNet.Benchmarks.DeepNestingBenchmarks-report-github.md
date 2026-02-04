```

BenchmarkDotNet v0.15.0, macOS 26.2 (25C56) [Darwin 25.2.0]
Apple M3 Max, 1 CPU, 16 logical and 16 physical cores
.NET SDK 10.0.100
  [Host]     : .NET 8.0.11 (8.0.1124.51707), Arm64 RyuJIT AdvSIMD
  Job-BJDRJN : .NET 8.0.11 (8.0.1124.51707), Arm64 RyuJIT AdvSIMD

IterationCount=10  WarmupCount=3  

```
| Method            | Mean       | Error     | StdDev    | Gen0    | Gen1    | Allocated |
|------------------ |-----------:|----------:|----------:|--------:|--------:|----------:|
| Parse_Depth10     |   8.177 μs | 0.1091 μs | 0.0649 μs |  2.8839 |  0.0305 |  23.59 KB |
| Parse_Depth25     |  24.544 μs | 0.2537 μs | 0.1510 μs |  6.1340 |  0.2136 |  50.27 KB |
| Parse_Depth50     |  65.100 μs | 2.9605 μs | 1.7618 μs | 16.1133 |  2.8076 |  132.1 KB |
| Parse_Depth75     | 119.016 μs | 2.4288 μs | 1.6065 μs | 32.2266 | 10.7422 | 265.46 KB |
| Encode_Depth10    |  11.253 μs | 0.5154 μs | 0.3409 μs |  3.3112 |  0.0458 |  27.11 KB |
| Encode_Depth50    |         NA |        NA |        NA |      NA |      NA |        NA |
| RoundTrip_Depth25 |  61.131 μs | 1.7561 μs | 1.1616 μs | 15.8691 |  1.4648 |  129.7 KB |
| RoundTrip_Depth50 |         NA |        NA |        NA |      NA |      NA |        NA |

Benchmarks with issues:
  DeepNestingBenchmarks.Encode_Depth50: Job-BJDRJN(IterationCount=10, WarmupCount=3)
  DeepNestingBenchmarks.RoundTrip_Depth50: Job-BJDRJN(IterationCount=10, WarmupCount=3)
