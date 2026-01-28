```

BenchmarkDotNet v0.15.0, macOS 26.2 (25C56) [Darwin 25.2.0]
Apple M3 Max, 1 CPU, 16 logical and 16 physical cores
.NET SDK 10.0.100
  [Host]     : .NET 8.0.11 (8.0.1124.51707), Arm64 RyuJIT AdvSIMD
  Job-KWXFVL : .NET 8.0.11 (8.0.1124.51707), Arm64 RyuJIT AdvSIMD

IterationCount=10  WarmupCount=3  

```
| Method            | Mean       | Error     | StdDev    | Gen0    | Gen1    | Allocated |
|------------------ |-----------:|----------:|----------:|--------:|--------:|----------:|
| Parse_Depth10     |   8.132 μs | 0.0490 μs | 0.0292 μs |  2.8839 |  0.0305 |  23.59 KB |
| Parse_Depth25     |  25.262 μs | 0.3031 μs | 0.2005 μs |  6.1340 |  0.0305 |  50.27 KB |
| Parse_Depth50     |  64.481 μs | 0.5854 μs | 0.3872 μs | 16.1133 |  2.8076 |  132.1 KB |
| Parse_Depth75     | 119.855 μs | 1.5517 μs | 1.0264 μs | 32.2266 | 10.7422 | 265.46 KB |
| Encode_Depth10    |  11.335 μs | 0.0866 μs | 0.0573 μs |  3.5248 |       - |  28.82 KB |
| Encode_Depth50    |         NA |        NA |        NA |      NA |      NA |        NA |
| RoundTrip_Depth25 |  60.457 μs | 0.6194 μs | 0.3240 μs | 16.3574 |  1.0986 | 133.87 KB |
| RoundTrip_Depth50 |         NA |        NA |        NA |      NA |      NA |        NA |

Benchmarks with issues:
  DeepNestingBenchmarks.Encode_Depth50: Job-KWXFVL(IterationCount=10, WarmupCount=3)
  DeepNestingBenchmarks.RoundTrip_Depth50: Job-KWXFVL(IterationCount=10, WarmupCount=3)
