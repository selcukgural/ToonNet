```

BenchmarkDotNet v0.15.0, macOS 26.2 (25C56) [Darwin 25.2.0]
Apple M3 Max, 1 CPU, 16 logical and 16 physical cores
.NET SDK 10.0.100
  [Host]     : .NET 8.0.11 (8.0.1124.51707), Arm64 RyuJIT AdvSIMD
  Job-BJDRJN : .NET 8.0.11 (8.0.1124.51707), Arm64 RyuJIT AdvSIMD

IterationCount=10  WarmupCount=3  

```
| Method                             | Mean       | Error     | StdDev    | Gen0    | Gen1    | Gen2    | Allocated |
|----------------------------------- |-----------:|----------:|----------:|--------:|--------:|--------:|----------:|
| SerializeAsync_Small               |   1.654 μs | 0.0168 μs | 0.0088 μs |  0.3738 |       - |       - |   3.06 KB |
| DeserializeAsync_Small             |         NA |        NA |        NA |      NA |      NA |      NA |        NA |
| ParseAsync_Small                   |   7.708 μs | 0.4103 μs | 0.2441 μs |  2.8229 |  0.0610 |       - |  21.82 KB |
| ParseAsync_Medium                  | 119.982 μs | 0.6666 μs | 0.3487 μs | 41.5039 | 41.5039 | 41.5039 | 343.98 KB |
| EncodeAsync_Small                  |  14.894 μs | 0.3408 μs | 0.2254 μs |  2.8839 |  0.0610 |       - |  23.41 KB |
| FileOperations_SerializeToFile     |  52.505 μs | 1.9789 μs | 1.3089 μs |  0.4272 |       - |       - |    3.8 KB |
| FileOperations_DeserializeFromFile |  76.725 μs | 3.5626 μs | 2.1200 μs |  2.8076 |  0.1221 |       - |  21.81 KB |
| StreamOperations_WriteAndRead      |   4.653 μs | 0.0209 μs | 0.0109 μs |  1.9455 |  0.0458 |       - |  15.92 KB |

Benchmarks with issues:
  AsyncBenchmarks.DeserializeAsync_Small: Job-BJDRJN(IterationCount=10, WarmupCount=3)
