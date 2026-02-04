```

BenchmarkDotNet v0.15.0, macOS 26.2 (25C56) [Darwin 25.2.0]
Apple M3 Max, 1 CPU, 16 logical and 16 physical cores
.NET SDK 10.0.100
  [Host]     : .NET 8.0.11 (8.0.1124.51707), Arm64 RyuJIT AdvSIMD
  Job-RBRYOQ : .NET 8.0.11 (8.0.1124.51707), Arm64 RyuJIT AdvSIMD

IterationCount=10  RunStrategy=Throughput  WarmupCount=3  

```
| Method                          | Mean         | Error        | StdDev     | Ratio          | RatioSD | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio  |
|-------------------------------- |-------------:|-------------:|-----------:|---------------:|--------:|--------:|--------:|--------:|----------:|-------------:|
| &#39;GetBytes - Small (100B)&#39;       |     18.08 ns |     0.143 ns |   0.075 ns |       baseline |         |  0.0181 |       - |       - |     152 B |              |
| &#39;GetBytes - Medium (1KB)&#39;       |     98.79 ns |     1.408 ns |   0.932 ns |   5.46x slower |   0.05x |  0.1625 |       - |       - |    1360 B |   8.95x more |
| &#39;GetBytes - Large (10KB)&#39;       |    878.04 ns |    32.449 ns |  21.463 ns |  48.57x slower |   1.15x |  1.5898 |       - |       - |   13328 B |  87.68x more |
| &#39;GetBytes - VeryLarge (100KB)&#39;  | 16,361.16 ns | 1,379.705 ns | 912.589 ns | 905.10x slower |  48.32x | 41.6565 | 41.6565 | 41.6565 |  133060 B | 875.39x more |
| &#39;ArrayPool - Small (100B)&#39;      |     15.61 ns |     0.189 ns |   0.125 ns |   1.16x faster |   0.01x |       - |       - |       - |         - |           NA |
| &#39;ArrayPool - Medium (1KB)&#39;      |     49.89 ns |     0.743 ns |   0.442 ns |   2.76x slower |   0.03x |       - |       - |       - |         - |           NA |
| &#39;ArrayPool - Large (10KB)&#39;      |    369.99 ns |     4.880 ns |   3.228 ns |  20.47x slower |   0.19x |       - |       - |       - |         - |           NA |
| &#39;ArrayPool - VeryLarge (100KB)&#39; |  3,717.52 ns |    14.639 ns |   8.711 ns | 205.65x slower |   0.92x |       - |       - |       - |       2 B |  76.00x less |
| &#39;StreamWrite_GetBytes - Large&#39;  |  1,453.96 ns |    22.445 ns |  13.356 ns |  80.43x slower |   0.77x |  3.2043 |  0.1526 |       - |   26792 B | 176.26x more |
| &#39;StreamWrite_ArrayPool - Large&#39; |    901.44 ns |     8.628 ns |   4.513 ns |  49.87x slower |   0.31x |  1.6069 |       - |       - |   13464 B |  88.58x more |
