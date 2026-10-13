# ToonNet vs Toon.Format – benchmark results

Measured with `dotnet run -c Release -- --filter "*" --launchCount 1 --warmupCount 3 --iterationCount 8` in this
directory on 2026-10-14.

```
BenchmarkDotNet v0.15.8, macOS Tahoe 26.6.2 (25G83) [Darwin 25.6.0]
Apple M3 Max, 1 CPU, 16 logical and 16 physical cores
.NET SDK 10.0.105, .NET 10.0.5 (Arm64 RyuJIT armv8.0-a)
Toon.Format 1.0.0, ToonNet 3.0.0
```

- **Employees:** flat records (tabular form)
- **Orders:** records with two nested objects and an array (list form)
- **Products:** records with uniform nested objects (tabular form with nested field groups)
- **Servers:** a dictionary of uniform records (keyed tabular form)

"ToonNet" uses `[ToonSerializable]` models (generated code); "ToonNet (reflection)" the same models without it.
Ratios are relative to Toon.Format. Before measuring, setup checks that ToonNet and Toon.Format write identical text
and read it back to the same values.

| Method                 | Categories  | Data      | Count | Mean          | Error         | StdDev        | Ratio         | RatioSD | Gen0       | Gen1      | Gen2      | Allocated   | Alloc Ratio |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Toon.Format            | Deserialize | Employees | 100   |    139.310 us |     2.1872 us |     0.9711 us |      baseline |         |    42.9688 |   11.7188 |         - |   350.97 KB |             |
| ToonNet                | Deserialize | Employees | 100   |     47.220 us |     0.5887 us |     0.2614 us |  2.95x faster |   0.02x |     3.3569 |    0.2441 |         - |    27.47 KB | 12.78x less |
| 'ToonNet (reflection)' | Deserialize | Employees | 100   |     51.002 us |     0.8787 us |     0.4596 us |  2.73x faster |   0.03x |     3.3569 |    0.2441 |         - |    27.44 KB | 12.79x less |
| System.Text.Json       | Deserialize | Employees | 100   |     34.256 us |     1.0523 us |     0.5504 us |  4.07x faster |   0.07x |     3.0518 |    0.1831 |         - |    25.18 KB | 13.94x less |
|                        |             |           |       |               |               |               |               |         |            |           |           |             |             |
| Toon.Format            | Deserialize | Employees | 10000 | 32,440.049 us |   432.0369 us |   191.8271 us |      baseline |         |  5000.0000 | 2250.0000 |  812.5000 | 35152.92 KB |             |
| ToonNet                | Deserialize | Employees | 10000 |  5,325.685 us |    40.2998 us |    17.8934 us |  6.09x faster |   0.04x |   281.2500 |  140.6250 |         - |  2347.78 KB | 14.97x less |
| 'ToonNet (reflection)' | Deserialize | Employees | 10000 |  5,278.017 us |    26.1763 us |    11.6224 us |  6.15x faster |   0.04x |   281.2500 |  140.6250 |         - |  2347.75 KB | 14.97x less |
| System.Text.Json       | Deserialize | Employees | 10000 |  4,246.561 us |    31.2615 us |    16.3504 us |  7.64x faster |   0.05x |   328.1250 |  203.1250 |   78.1250 |  2522.71 KB | 13.93x less |
|                        |             |           |       |               |               |               |               |         |            |           |           |             |             |
| Toon.Format            | Deserialize | Orders    | 100   |    254.080 us |     9.9323 us |     5.1948 us |      baseline |         |    97.6563 |   37.1094 |         - |   809.82 KB |             |
| ToonNet                | Deserialize | Orders    | 100   |     81.099 us |     1.1218 us |     0.4981 us |  3.13x faster |   0.06x |     7.9346 |    0.9766 |         - |    64.84 KB | 12.49x less |
| 'ToonNet (reflection)' | Deserialize | Orders    | 100   |     93.661 us |     1.4890 us |     0.7788 us |  2.71x faster |   0.06x |     7.9346 |    1.0986 |         - |    65.58 KB | 12.35x less |
| System.Text.Json       | Deserialize | Orders    | 100   |     51.587 us |     0.6930 us |     0.3077 us |  4.93x faster |   0.10x |     7.1411 |    0.9766 |         - |    58.77 KB | 13.78x less |
|                        |             |           |       |               |               |               |               |         |            |           |           |             |             |
| Toon.Format            | Deserialize | Orders    | 10000 | 75,544.790 us | 3,353.6791 us | 1,489.0543 us |      baseline |         | 10857.1429 | 5142.8571 | 1714.2857 | 80186.84 KB |             |
| ToonNet                | Deserialize | Orders    | 10000 |  9,058.039 us |   232.0512 us |   121.3673 us |  8.34x faster |   0.19x |   671.8750 |  328.1250 |         - |  5550.05 KB | 14.45x less |
| 'ToonNet (reflection)' | Deserialize | Orders    | 10000 | 10,475.802 us |   130.3796 us |    57.8893 us |  7.21x faster |   0.14x |   750.0000 |  375.0000 |         - |  6174.96 KB | 12.99x less |
| System.Text.Json       | Deserialize | Orders    | 10000 |  8,663.922 us |    91.4215 us |    40.5917 us |  8.72x faster |   0.17x |   812.5000 |  500.0000 |  187.5000 |  5882.29 KB | 13.63x less |
|                        |             |           |       |               |               |               |               |         |            |           |           |             |             |
| Toon.Format            | Deserialize | Products  | 100   |    143.840 us |     2.0339 us |     1.0637 us |      baseline |         |    36.1328 |    9.7656 |         - |   295.87 KB |             |
| ToonNet                | Deserialize | Products  | 100   |     42.078 us |     0.1517 us |     0.0673 us |  3.42x faster |   0.02x |     4.6387 |    0.4883 |         - |    38.29 KB |  7.73x less |
| 'ToonNet (reflection)' | Deserialize | Products  | 100   |     51.341 us |     0.5431 us |     0.2840 us |  2.80x faster |   0.02x |     4.6387 |    0.4272 |         - |    38.26 KB |  7.73x less |
| System.Text.Json       | Deserialize | Products  | 100   |     43.130 us |     0.6407 us |     0.2845 us |  3.34x faster |   0.03x |     4.3945 |    0.3662 |         - |    36.12 KB |  8.19x less |
|                        |             |           |       |               |               |               |               |         |            |           |           |             |             |
| Toon.Format            | Deserialize | Products  | 10000 | 34,947.250 us | 1,078.1407 us |   563.8884 us |      baseline |         |  4333.3333 | 2133.3333 |  866.6667 | 29471.41 KB |             |
| ToonNet                | Deserialize | Products  | 10000 |  4,868.442 us |    63.5526 us |    28.2177 us |  7.18x faster |   0.12x |   414.0625 |  203.1250 |         - |  3441.41 KB |  8.56x less |
| 'ToonNet (reflection)' | Deserialize | Products  | 10000 |  5,667.572 us |   173.5361 us |    90.7628 us |  6.17x faster |   0.13x |   414.0625 |  203.1250 |         - |  3441.38 KB |  8.56x less |
| System.Text.Json       | Deserialize | Products  | 10000 |  6,497.446 us |   112.6858 us |    58.9369 us |  5.38x faster |   0.09x |   484.3750 |  218.7500 |   78.1250 |  3616.52 KB |  8.15x less |
|                        |             |           |       |               |               |               |               |         |            |           |           |             |             |
| Toon.Format            | Deserialize | Servers   | 100   |     66.359 us |     0.9690 us |     0.5068 us |      baseline |         |    26.8555 |    5.8594 |         - |    223.3 KB |             |
| ToonNet                | Deserialize | Servers   | 100   |     20.813 us |     0.0697 us |     0.0309 us |  3.19x faster |   0.02x |     4.3640 |    0.3967 |         - |     35.8 KB |  6.24x less |
| 'ToonNet (reflection)' | Deserialize | Servers   | 100   |     23.411 us |     0.1912 us |     0.0849 us |  2.83x faster |   0.02x |     4.3640 |    0.4272 |         - |    35.77 KB |  6.24x less |
| System.Text.Json       | Deserialize | Servers   | 100   |     17.807 us |     0.0885 us |     0.0393 us |  3.73x faster |   0.03x |     3.1738 |    0.2441 |         - |    26.04 KB |  8.58x less |
|                        |             |           |       |               |               |               |               |         |            |           |           |             |             |
| Toon.Format            | Deserialize | Servers   | 10000 | 15,742.927 us |   162.2490 us |    84.8594 us |      baseline |         |  2843.7500 | 1437.5000 |  531.2500 | 22392.88 KB |             |
| ToonNet                | Deserialize | Servers   | 10000 |  3,354.786 us |    80.1822 us |    41.9368 us |  4.69x faster |   0.06x |   402.3438 |  281.2500 |  160.1563 |     3211 KB |  6.97x less |
| 'ToonNet (reflection)' | Deserialize | Servers   | 10000 |  3,718.140 us |    59.1492 us |    26.2626 us |  4.23x faster |   0.04x |   402.3438 |  285.1563 |  156.2500 |  3210.95 KB |  6.97x less |
| System.Text.Json       | Deserialize | Servers   | 10000 |  2,849.936 us |    59.9066 us |    26.5989 us |  5.52x faster |   0.06x |   281.2500 |  156.2500 |   74.2188 |  2551.17 KB |  8.78x less |
|                        |             |           |       |               |               |               |               |         |            |           |           |             |             |
| Toon.Format            | Serialize   | Employees | 100   |    236.920 us |     0.8868 us |     0.3938 us |      baseline |         |    62.5000 |   27.3438 |         - |   525.56 KB |             |
| ToonNet                | Serialize   | Employees | 100   |     18.385 us |     0.1873 us |     0.0979 us | 12.89x faster |   0.07x |     2.0447 |         - |         - |    16.77 KB | 31.35x less |
| 'ToonNet (reflection)' | Serialize   | Employees | 100   |     24.585 us |     0.2513 us |     0.1116 us |  9.64x faster |   0.04x |     2.0447 |         - |         - |    16.77 KB | 31.35x less |
| System.Text.Json       | Serialize   | Employees | 100   |     20.970 us |     0.2225 us |     0.0988 us | 11.30x faster |   0.05x |     4.0283 |         - |         - |    33.19 KB | 15.84x less |
|                        |             |           |       |               |               |               |               |         |            |           |           |             |             |
| Toon.Format            | Serialize   | Employees | 10000 | 47,323.084 us | 1,578.6093 us |   700.9123 us |      baseline |         |  6818.1818 | 3181.8182 | 1000.0000 | 53017.24 KB |             |
| ToonNet                | Serialize   | Employees | 10000 |  1,997.780 us |    18.1565 us |     8.0616 us | 23.69x faster |   0.34x |   152.3438 |  152.3438 |  152.3438 |  1744.19 KB | 30.40x less |
| 'ToonNet (reflection)' | Serialize   | Employees | 10000 |  2,661.421 us |    23.6805 us |    12.3853 us | 17.78x faster |   0.26x |   152.3438 |  152.3438 |  152.3438 |  1744.27 KB | 30.40x less |
| System.Text.Json       | Serialize   | Employees | 10000 |  2,279.762 us |    26.6856 us |    13.9571 us | 20.76x faster |   0.31x |   242.1875 |  242.1875 |  242.1875 |  3404.85 KB | 15.57x less |
|                        |             |           |       |               |               |               |               |         |            |           |           |             |             |
| Toon.Format            | Serialize   | Orders    | 100   |    350.205 us |    38.5915 us |    20.1841 us |      baseline |         |   105.4688 |   35.1563 |         - |   876.35 KB |             |
| ToonNet                | Serialize   | Orders    | 100   |     25.994 us |     0.3243 us |     0.1440 us | 13.47x faster |   0.74x |     5.6152 |         - |         - |    46.09 KB | 19.01x less |
| 'ToonNet (reflection)' | Serialize   | Orders    | 100   |     45.157 us |     1.0924 us |     0.5713 us |  7.76x faster |   0.43x |     6.0425 |         - |         - |       50 KB | 17.53x less |
| System.Text.Json       | Serialize   | Orders    | 100   |     26.834 us |     0.3782 us |     0.1978 us | 13.05x faster |   0.71x |     4.9744 |         - |         - |    40.96 KB | 21.39x less |
|                        |             |           |       |               |               |               |               |         |            |           |           |             |             |
| Toon.Format            | Serialize   | Orders    | 10000 | 79,320.328 us | 2,238.4929 us | 1,170.7751 us |      baseline |         | 10333.3333 | 4000.0000 | 1000.0000 | 87005.79 KB |             |
| ToonNet                | Serialize   | Orders    | 10000 |  2,793.534 us |    41.7624 us |    18.5428 us | 28.40x faster |   0.43x |   167.9688 |  167.9688 |  167.9688 |  4705.62 KB | 18.49x less |
| 'ToonNet (reflection)' | Serialize   | Orders    | 10000 |  4,779.546 us |   119.1375 us |    62.3112 us | 16.60x faster |   0.31x |   203.1250 |  156.2500 |  156.2500 |  5095.28 KB | 17.08x less |
| System.Text.Json       | Serialize   | Orders    | 10000 |  2,874.760 us |    85.3153 us |    44.6216 us | 27.60x faster |   0.55x |   152.3438 |  152.3438 |  152.3438 |  4197.58 KB | 20.73x less |
|                        |             |           |       |               |               |               |               |         |            |           |           |             |             |
| Toon.Format            | Serialize   | Products  | 100   |    254.809 us |     5.7139 us |     2.5370 us |      baseline |         |    66.4063 |   21.4844 |         - |   545.25 KB |             |
| ToonNet                | Serialize   | Products  | 100   |     27.058 us |     0.4022 us |     0.1786 us |  9.42x faster |   0.10x |     2.1362 |         - |         - |    17.66 KB | 30.87x less |
| 'ToonNet (reflection)' | Serialize   | Products  | 100   |     31.378 us |     0.2070 us |     0.0919 us |  8.12x faster |   0.08x |     1.8921 |         - |         - |    15.91 KB | 34.28x less |
| System.Text.Json       | Serialize   | Products  | 100   |     27.776 us |     0.4145 us |     0.1840 us |  9.17x faster |   0.10x |     3.3264 |         - |         - |    27.45 KB | 19.86x less |
|                        |             |           |       |               |               |               |               |         |            |           |           |             |             |
| Toon.Format            | Serialize   | Products  | 10000 | 53,109.865 us | 2,126.3715 us |   944.1221 us |      baseline |         |  6900.0000 | 3000.0000 |  900.0000 | 54374.75 KB |             |
| ToonNet                | Serialize   | Products  | 10000 |  2,863.161 us |    33.2807 us |    14.7769 us | 18.55x faster |   0.32x |   167.9688 |  125.0000 |  101.5625 |  1557.03 KB | 34.92x less |
| 'ToonNet (reflection)' | Serialize   | Products  | 10000 |  3,220.387 us |    48.1379 us |    21.3735 us | 16.49x faster |   0.29x |   160.1563 |  121.0938 |  105.4688 |  1458.66 KB | 37.28x less |
| System.Text.Json       | Serialize   | Products  | 10000 |  3,004.360 us |    21.3280 us |     9.4698 us | 17.68x faster |   0.30x |   273.4375 |  273.4375 |  273.4375 |  2767.33 KB | 19.65x less |
|                        |             |           |       |               |               |               |               |         |            |           |           |             |             |
| Toon.Format            | Serialize   | Servers   | 100   |    106.101 us |     2.0672 us |     0.7372 us |      baseline |         |    34.1797 |    7.8125 |         - |   281.51 KB |             |
| ToonNet                | Serialize   | Servers   | 100   |      9.015 us |     0.1734 us |     0.0907 us | 11.77x faster |   0.13x |     1.8616 |    0.0153 |         - |    15.28 KB | 18.42x less |
| 'ToonNet (reflection)' | Serialize   | Servers   | 100   |      9.684 us |     0.1110 us |     0.0493 us | 10.96x faster |   0.09x |     1.7700 |    0.0153 |         - |    14.51 KB | 19.40x less |
| System.Text.Json       | Serialize   | Servers   | 100   |      8.657 us |     0.0693 us |     0.0308 us | 12.26x faster |   0.09x |     1.8768 |         - |         - |    15.37 KB | 18.32x less |
|                        |             |           |       |               |               |               |               |         |            |           |           |             |             |
| Toon.Format            | Serialize   | Servers   | 10000 | 24,078.866 us |   869.2981 us |   454.6597 us |      baseline |         |  3812.5000 | 1593.7500 |  562.5000 | 28252.29 KB |             |
| ToonNet                | Serialize   | Servers   | 10000 |    938.563 us |    29.7279 us |    15.5483 us | 25.66x faster |   0.60x |   197.2656 |  169.9219 |  158.2031 |  1577.54 KB | 17.91x less |
| 'ToonNet (reflection)' | Serialize   | Servers   | 10000 |  1,008.027 us |    19.2073 us |    10.0458 us | 23.89x faster |   0.48x |   197.2656 |  166.0156 |  162.1094 |  1538.19 KB | 18.37x less |
| System.Text.Json       | Serialize   | Servers   | 10000 |    937.728 us |     3.3695 us |     1.2016 us | 25.68x faster |   0.46x |   247.0703 |  247.0703 |  247.0703 |  1572.97 KB | 17.96x less |
