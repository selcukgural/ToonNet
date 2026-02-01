```

BenchmarkDotNet v0.15.0, macOS 26.2 (25C56) [Darwin 25.2.0]
Apple M3 Max, 1 CPU, 16 logical and 16 physical cores
.NET SDK 10.0.100
  [Host]     : .NET 8.0.11 (8.0.1124.51707), Arm64 RyuJIT AdvSIMD
  Job-TUDSHG : .NET 8.0.11 (8.0.1124.51707), Arm64 RyuJIT AdvSIMD

IterationCount=10  WarmupCount=3  

```
| Method                            | Mean         | Error        | StdDev       | Gen0      | Gen1      | Gen2      | Allocated   |
|---------------------------------- |-------------:|-------------:|-------------:|----------:|----------:|----------:|------------:|
| Parse_HighAllocationPressure      |  2,951.94 μs |    39.826 μs |    26.342 μs |  613.2813 |  531.2500 |  347.6563 |  4111.53 KB |
| Encode_HighAllocationPressure     |  4,184.48 μs |    50.552 μs |    33.437 μs |  906.2500 |  882.8125 |  500.0000 |  5999.88 KB |
| RoundTrip_HighAllocationPressure  |  7,143.55 μs |    25.076 μs |    13.115 μs | 1640.6250 | 1625.0000 |  968.7500 | 10111.49 KB |
| Parse_MultipleDocuments           | 29,470.06 μs | 1,783.788 μs | 1,179.865 μs | 6093.7500 | 5312.5000 | 3437.5000 | 41115.68 KB |
| Encode_MultipleDocuments          | 12,778.37 μs |   556.793 μs |   368.284 μs | 3015.6250 | 2953.1250 | 1984.3750 | 22995.69 KB |
| CreateAndDispose_ParserInstances  |     38.28 μs |     1.498 μs |     0.991 μs |   15.9302 |         - |         - |   130.47 KB |
| CreateAndDispose_EncoderInstances |     16.10 μs |     0.194 μs |     0.128 μs |    2.0447 |         - |         - |    16.71 KB |
