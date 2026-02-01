```

BenchmarkDotNet v0.15.0, macOS 26.2 (25C56) [Darwin 25.2.0]
Apple M3 Max, 1 CPU, 16 logical and 16 physical cores
.NET SDK 10.0.100
  [Host]     : .NET 8.0.11 (8.0.1124.51707), Arm64 RyuJIT AdvSIMD
  Job-JQGDCJ : .NET 8.0.11 (8.0.1124.51707), Arm64 RyuJIT AdvSIMD

IterationCount=10  WarmupCount=3  

```
| Method                             | Mean       | Error      | StdDev    | Gen0     | Gen1     | Gen2    | Allocated |
|----------------------------------- |-----------:|-----------:|----------:|---------:|---------:|--------:|----------:|
| SerializeAsync_Small               |   1.798 μs |  0.0357 μs | 0.0212 μs |   0.3738 |        - |       - |   3.06 KB |
| DeserializeAsync_Small             |         NA |         NA |        NA |       NA |       NA |      NA |        NA |
| ParseAsync_Small                   |   8.379 μs |  0.3885 μs | 0.2032 μs |   2.8381 |   0.0610 |       - |  21.82 KB |
| ParseAsync_Medium                  | 132.097 μs |  4.1397 μs | 2.4635 μs | 131.5918 | 131.5918 | 37.5977 | 343.97 KB |
| EncodeAsync_Small                  |  10.009 μs |  0.6227 μs | 0.4119 μs |   2.8534 |   0.0305 |       - |  23.05 KB |
| FileOperations_SerializeToFile     |  57.401 μs | 13.0804 μs | 8.6519 μs |   0.3662 |        - |       - |    3.8 KB |
| FileOperations_DeserializeFromFile |  77.843 μs |  4.4337 μs | 2.3189 μs |   2.9297 |        - |       - |  21.81 KB |
| StreamOperations_WriteAndRead      |   4.683 μs |  0.0734 μs | 0.0437 μs |   1.9684 |   0.0305 |       - |   16.1 KB |

Benchmarks with issues:
  AsyncBenchmarks.DeserializeAsync_Small: Job-JQGDCJ(IterationCount=10, WarmupCount=3)
