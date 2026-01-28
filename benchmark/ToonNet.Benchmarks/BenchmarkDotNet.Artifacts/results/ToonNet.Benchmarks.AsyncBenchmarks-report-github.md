```

BenchmarkDotNet v0.15.0, macOS 26.2 (25C56) [Darwin 25.2.0]
Apple M3 Max, 1 CPU, 16 logical and 16 physical cores
.NET SDK 10.0.100
  [Host]     : .NET 8.0.11 (8.0.1124.51707), Arm64 RyuJIT AdvSIMD
  Job-KWXFVL : .NET 8.0.11 (8.0.1124.51707), Arm64 RyuJIT AdvSIMD

IterationCount=10  WarmupCount=3  

```
| Method                             | Mean       | Error     | StdDev    | Gen0     | Gen1     | Gen2    | Allocated |
|----------------------------------- |-----------:|----------:|----------:|---------:|---------:|--------:|----------:|
| SerializeAsync_Small               |   1.924 μs | 0.0154 μs | 0.0092 μs |   0.4272 |        - |       - |    3.5 KB |
| DeserializeAsync_Small             |         NA |        NA |        NA |       NA |       NA |      NA |        NA |
| ParseAsync_Small                   |   8.890 μs | 0.6438 μs | 0.3831 μs |   2.8229 |   0.0610 |       - |  21.83 KB |
| ParseAsync_Medium                  | 133.333 μs | 2.9965 μs | 1.9820 μs | 131.5918 | 131.5918 | 37.5977 | 343.97 KB |
| EncodeAsync_Small                  |  11.008 μs | 0.8664 μs | 0.5731 μs |   3.0670 |   0.0305 |       - |  24.62 KB |
| FileOperations_SerializeToFile     |  59.837 μs | 5.7993 μs | 3.8359 μs |   0.4883 |        - |       - |   4.23 KB |
| FileOperations_DeserializeFromFile |  82.516 μs | 6.2636 μs | 4.1430 μs |   2.9297 |   0.1221 |       - |  22.25 KB |
| StreamOperations_WriteAndRead      |   4.973 μs | 0.0618 μs | 0.0368 μs |   2.0218 |   0.0076 |       - |  16.54 KB |

Benchmarks with issues:
  AsyncBenchmarks.DeserializeAsync_Small: Job-KWXFVL(IterationCount=10, WarmupCount=3)
