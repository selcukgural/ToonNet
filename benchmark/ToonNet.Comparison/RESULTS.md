# ToonNet vs Toon.Format – benchmark results

Measured with `dotnet run -c Release -- --filter "*" --launchCount 1 --warmupCount 3 --iterationCount 8` in this
directory on 2026-10-15 (one run for both tables).

```
BenchmarkDotNet v0.15.8, macOS Tahoe 26.6.2 (25G83) [Darwin 25.6.0]
Apple M3 Max, 1 CPU, 16 logical and 16 physical cores
.NET SDK 10.0.105, .NET 10.0.5 (Arm64 RyuJIT armv8.0-a)
Toon.Format 1.0.0, ToonNet 4.0.0
```

- **Employees:** flat records (tabular form)
- **Orders:** records with two nested objects and an array (list form)
- **Products:** records with uniform nested objects (tabular form with nested field groups)
- **Servers:** a dictionary of uniform records (keyed tabular form)

"ToonNet" uses `[ToonSerializable]` models (generated code); "ToonNet (type info)" the same models through the
overloads that take a `ToonTypeInfo` (no reflection, for trimming and NativeAOT); "ToonNet (reflection)" the same models
without generated code.
Ratios are relative to Toon.Format. Before measuring, setup checks that ToonNet and Toon.Format write identical text
and read it back to the same values.

| Method                 | Categories  | Data      | Count | Mean          | Error         | StdDev        | Ratio         | RatioSD | Gen0       | Gen1      | Gen2      | Allocated   | Alloc Ratio |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Toon.Format            | Deserialize | Employees | 100   |    137.438 us |     1.1422 us |     0.5974 us |      baseline |         |    42.7246 |   11.7188 |         - |   350.97 KB |             |
| ToonNet                | Deserialize | Employees | 100   |     48.942 us |     0.6011 us |     0.3144 us |  2.81x faster |   0.02x |     3.3569 |    0.0610 |         - |    27.51 KB | 12.76x less |
| 'ToonNet (reflection)' | Deserialize | Employees | 100   |     56.622 us |     0.3592 us |     0.1595 us |  2.43x faster |   0.01x |     3.3569 |    0.2441 |         - |    27.48 KB | 12.77x less |
| 'ToonNet (type info)'  | Deserialize | Employees | 100   |     46.000 us |     0.8765 us |     0.4584 us |  2.99x faster |   0.03x |     3.3569 |    0.2441 |         - |    27.63 KB | 12.70x less |
| System.Text.Json       | Deserialize | Employees | 100   |     33.412 us |     0.1821 us |     0.0809 us |  4.11x faster |   0.02x |     3.0518 |    0.1831 |         - |    25.18 KB | 13.94x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Toon.Format            | Deserialize | Employees | 10000 | 31,583.228 us |   675.1695 us |   299.7794 us |      baseline |         |  5000.0000 | 2281.2500 |  843.7500 | 35153.01 KB |             |
| ToonNet                | Deserialize | Employees | 10000 |  5,267.091 us |    45.7533 us |    23.9299 us |  6.00x faster |   0.06x |   281.2500 |  140.6250 |         - |  2347.82 KB | 14.97x less |
| 'ToonNet (reflection)' | Deserialize | Employees | 10000 |  5,544.484 us |    48.1597 us |    25.1884 us |  5.70x faster |   0.06x |   281.2500 |  140.6250 |         - |  2347.79 KB | 14.97x less |
| 'ToonNet (type info)'  | Deserialize | Employees | 10000 |  5,092.890 us |    31.1573 us |    13.8340 us |  6.20x faster |   0.06x |   281.2500 |  140.6250 |         - |  2347.95 KB | 14.97x less |
| System.Text.Json       | Deserialize | Employees | 10000 |  4,192.371 us |    45.8459 us |    23.9783 us |  7.53x faster |   0.08x |   328.1250 |  203.1250 |   78.1250 |  2522.72 KB | 13.93x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Toon.Format            | Deserialize | Orders    | 100   |    250.387 us |     2.8548 us |     1.4931 us |      baseline |         |    97.6563 |   37.1094 |         - |   809.82 KB |             |
| ToonNet                | Deserialize | Orders    | 100   |     80.504 us |     1.9539 us |     1.0219 us |  3.11x faster |   0.04x |     7.0801 |    0.9766 |         - |    58.69 KB | 13.80x less |
| 'ToonNet (reflection)' | Deserialize | Orders    | 100   |     96.813 us |     1.0118 us |     0.5292 us |  2.59x faster |   0.02x |     7.0801 |    0.9766 |         - |    58.66 KB | 13.81x less |
| 'ToonNet (type info)'  | Deserialize | Orders    | 100   |     80.813 us |     0.7149 us |     0.3174 us |  3.10x faster |   0.02x |     7.0801 |    0.8545 |         - |    58.81 KB | 13.77x less |
| System.Text.Json       | Deserialize | Orders    | 100   |     51.293 us |     0.4286 us |     0.1903 us |  4.88x faster |   0.03x |     7.1411 |    0.9766 |         - |    58.77 KB | 13.78x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Toon.Format            | Deserialize | Orders    | 10000 | 74,145.472 us | 3,896.7783 us | 2,038.0904 us |      baseline |         | 10571.4286 | 4857.1429 | 1571.4286 | 80186.48 KB |             |
| ToonNet                | Deserialize | Orders    | 10000 |  9,092.896 us |    67.1295 us |    35.1100 us |  8.15x faster |   0.21x |   671.8750 |  328.1250 |         - |  5550.09 KB | 14.45x less |
| 'ToonNet (reflection)' | Deserialize | Orders    | 10000 | 10,352.247 us |    78.4750 us |    41.0440 us |  7.16x faster |   0.19x |   671.8750 |  328.1250 |         - |  5550.06 KB | 14.45x less |
| 'ToonNet (type info)'  | Deserialize | Orders    | 10000 |  8,935.927 us |    35.2576 us |    18.4404 us |  8.30x faster |   0.22x |   671.8750 |  328.1250 |         - |  5550.22 KB | 14.45x less |
| System.Text.Json       | Deserialize | Orders    | 10000 |  8,487.426 us |   178.2469 us |    79.1427 us |  8.74x faster |   0.24x |   812.5000 |  500.0000 |  187.5000 |  5882.29 KB | 13.63x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Toon.Format            | Deserialize | Products  | 100   |    146.346 us |     3.7522 us |     1.9625 us |      baseline |         |    36.1328 |    9.7656 |         - |   295.87 KB |             |
| ToonNet                | Deserialize | Products  | 100   |     44.313 us |     0.8506 us |     0.4449 us |  3.30x faster |   0.05x |     4.6387 |    0.4272 |         - |    38.33 KB |  7.72x less |
| 'ToonNet (reflection)' | Deserialize | Products  | 100   |     59.679 us |     0.8857 us |     0.3932 us |  2.45x faster |   0.03x |     4.6387 |    0.4272 |         - |     38.3 KB |  7.73x less |
| 'ToonNet (type info)'  | Deserialize | Products  | 100   |     40.978 us |     0.9014 us |     0.4002 us |  3.57x faster |   0.06x |     4.6997 |    0.3662 |         - |    38.45 KB |  7.69x less |
| System.Text.Json       | Deserialize | Products  | 100   |     42.533 us |     0.8610 us |     0.3823 us |  3.44x faster |   0.05x |     4.3945 |    0.3662 |         - |    36.12 KB |  8.19x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Toon.Format            | Deserialize | Products  | 10000 | 34,473.333 us | 1,388.2915 us |   616.4100 us |      baseline |         |  4333.3333 | 2133.3333 |  866.6667 | 29471.35 KB |             |
| ToonNet                | Deserialize | Products  | 10000 |  5,100.334 us |   132.8758 us |    69.4966 us |  6.76x faster |   0.14x |   414.0625 |  203.1250 |         - |  3441.45 KB |  8.56x less |
| 'ToonNet (reflection)' | Deserialize | Products  | 10000 |  5,782.491 us |    45.2805 us |    16.1475 us |  5.96x faster |   0.10x |   414.0625 |  203.1250 |         - |  3441.42 KB |  8.56x less |
| 'ToonNet (type info)'  | Deserialize | Products  | 10000 |  4,676.400 us |   124.0254 us |    55.0680 us |  7.37x faster |   0.15x |   414.0625 |  203.1250 |         - |  3441.58 KB |  8.56x less |
| System.Text.Json       | Deserialize | Products  | 10000 |  6,347.928 us |   135.3228 us |    60.0842 us |  5.43x faster |   0.10x |   484.3750 |  218.7500 |   78.1250 |   3616.3 KB |  8.15x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Toon.Format            | Deserialize | Servers   | 100   |     67.081 us |     1.5342 us |     0.8024 us |      baseline |         |    26.8555 |    5.8594 |         - |    223.3 KB |             |
| ToonNet                | Deserialize | Servers   | 100   |     21.899 us |     0.1737 us |     0.0771 us |  3.06x faster |   0.04x |     4.3640 |    0.4272 |         - |    35.84 KB |  6.23x less |
| 'ToonNet (reflection)' | Deserialize | Servers   | 100   |     25.369 us |     0.5652 us |     0.2956 us |  2.64x faster |   0.04x |     4.3640 |    0.3357 |         - |    35.81 KB |  6.24x less |
| 'ToonNet (type info)'  | Deserialize | Servers   | 100   |     20.182 us |     0.4169 us |     0.2180 us |  3.32x faster |   0.05x |     4.3945 |    0.3662 |         - |    35.97 KB |  6.21x less |
| System.Text.Json       | Deserialize | Servers   | 100   |     17.839 us |     0.2239 us |     0.0994 us |  3.76x faster |   0.05x |     3.1738 |    0.2441 |         - |    26.04 KB |  8.58x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Toon.Format            | Deserialize | Servers   | 10000 | 15,931.845 us |   915.3895 us |   478.7664 us |      baseline |         |  2812.5000 | 1437.5000 |  500.0000 | 22393.02 KB |             |
| ToonNet                | Deserialize | Servers   | 10000 |  3,617.746 us |    87.7233 us |    45.8810 us |  4.40x faster |   0.14x |   398.4375 |  261.7188 |  160.1563 |   3211.1 KB |  6.97x less |
| 'ToonNet (reflection)' | Deserialize | Servers   | 10000 |  3,695.117 us |    44.4211 us |    19.7232 us |  4.31x faster |   0.12x |   394.5313 |  253.9063 |  160.1563 |  3211.06 KB |  6.97x less |
| 'ToonNet (type info)'  | Deserialize | Servers   | 10000 |  3,316.509 us |    69.7131 us |    30.9530 us |  4.80x faster |   0.14x |   402.3438 |  265.6250 |  160.1563 |  3211.26 KB |  6.97x less |
| System.Text.Json       | Deserialize | Servers   | 10000 |  2,818.944 us |    70.5509 us |    36.8995 us |  5.65x faster |   0.17x |   277.3438 |  156.2500 |   74.2188 |  2551.21 KB |  8.78x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Toon.Format            | Serialize   | Employees | 100   |    236.280 us |     2.3517 us |     1.2300 us |      baseline |         |    62.5000 |   27.3438 |         - |   525.56 KB |             |
| ToonNet                | Serialize   | Employees | 100   |     18.559 us |     0.4284 us |     0.2241 us | 12.73x faster |   0.16x |     2.0447 |         - |         - |    16.81 KB | 31.26x less |
| 'ToonNet (reflection)' | Serialize   | Employees | 100   |     25.131 us |     0.1595 us |     0.0708 us |  9.40x faster |   0.05x |     2.0447 |         - |         - |    16.81 KB | 31.26x less |
| 'ToonNet (type info)'  | Serialize   | Employees | 100   |     18.464 us |     0.1640 us |     0.0728 us | 12.80x faster |   0.08x |     2.0447 |         - |         - |    16.94 KB | 31.03x less |
| System.Text.Json       | Serialize   | Employees | 100   |     20.838 us |     0.1070 us |     0.0560 us | 11.34x faster |   0.06x |     4.0283 |         - |         - |    33.19 KB | 15.84x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Toon.Format            | Serialize   | Employees | 10000 | 46,034.062 us |   319.0058 us |   166.8462 us |      baseline |         |  6818.1818 | 3181.8182 | 1000.0000 | 53017.23 KB |             |
| ToonNet                | Serialize   | Employees | 10000 |  2,002.396 us |     6.4666 us |     2.3060 us | 22.99x faster |   0.08x |   152.3438 |  152.3438 |  152.3438 |  1744.21 KB | 30.40x less |
| 'ToonNet (reflection)' | Serialize   | Employees | 10000 |  2,641.039 us |    25.3889 us |    13.2789 us | 17.43x faster |   0.10x |   152.3438 |  152.3438 |  152.3438 |  1744.33 KB | 30.39x less |
| 'ToonNet (type info)'  | Serialize   | Employees | 10000 |  2,013.400 us |    10.7001 us |     5.5963 us | 22.86x faster |   0.10x |   152.3438 |  152.3438 |  152.3438 |  1744.32 KB | 30.39x less |
| System.Text.Json       | Serialize   | Employees | 10000 |  2,320.248 us |    39.7702 us |    17.6582 us | 19.84x faster |   0.16x |   242.1875 |  242.1875 |  242.1875 |  3404.85 KB | 15.57x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Toon.Format            | Serialize   | Orders    | 100   |    325.655 us |     2.5057 us |     1.3106 us |      baseline |         |   105.4688 |   35.1563 |         - |   876.35 KB |             |
| ToonNet                | Serialize   | Orders    | 100   |     25.849 us |     0.2836 us |     0.1259 us | 12.60x faster |   0.07x |     5.6152 |         - |         - |    46.14 KB | 18.99x less |
| 'ToonNet (reflection)' | Serialize   | Orders    | 100   |     39.410 us |     0.7780 us |     0.4069 us |  8.26x faster |   0.09x |     5.6152 |         - |         - |    46.14 KB | 18.99x less |
| 'ToonNet (type info)'  | Serialize   | Orders    | 100   |     25.977 us |     0.1234 us |     0.0548 us | 12.54x faster |   0.05x |     5.6458 |         - |         - |    46.27 KB | 18.94x less |
| System.Text.Json       | Serialize   | Orders    | 100   |     26.858 us |     0.5773 us |     0.3019 us | 12.13x faster |   0.14x |     4.9744 |         - |         - |    40.96 KB | 21.39x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Toon.Format            | Serialize   | Orders    | 10000 | 75,268.835 us | 1,239.9234 us |   648.5039 us |      baseline |         | 10333.3333 | 4000.0000 | 1000.0000 | 87005.79 KB |             |
| ToonNet                | Serialize   | Orders    | 10000 |  2,775.154 us |    19.1024 us |     9.9909 us | 27.12x faster |   0.24x |   156.2500 |  156.2500 |  156.2500 |   4705.6 KB | 18.49x less |
| 'ToonNet (reflection)' | Serialize   | Orders    | 10000 |  4,029.779 us |    26.2374 us |    13.7227 us | 18.68x faster |   0.16x |   140.6250 |  140.6250 |  140.6250 |  4705.45 KB | 18.49x less |
| 'ToonNet (type info)'  | Serialize   | Orders    | 10000 |  2,753.201 us |     8.0784 us |     3.5869 us | 27.34x faster |   0.22x |   156.2500 |  156.2500 |  156.2500 |  4705.74 KB | 18.49x less |
| System.Text.Json       | Serialize   | Orders    | 10000 |  2,874.842 us |    50.7450 us |    26.5406 us | 26.18x faster |   0.31x |   140.6250 |  140.6250 |  140.6250 |  4197.46 KB | 20.73x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Toon.Format            | Serialize   | Products  | 100   |    263.993 us |     6.9872 us |     3.1024 us |      baseline |         |    66.4063 |   21.4844 |         - |   545.25 KB |             |
| ToonNet                | Serialize   | Products  | 100   |     26.625 us |     0.1312 us |     0.0583 us |  9.92x faster |   0.11x |     1.4343 |         - |         - |    11.84 KB | 46.07x less |
| 'ToonNet (reflection)' | Serialize   | Products  | 100   |     31.573 us |     1.1745 us |     0.6143 us |  8.36x faster |   0.18x |     1.3428 |         - |         - |    11.13 KB | 48.98x less |
| 'ToonNet (type info)'  | Serialize   | Products  | 100   |     25.897 us |     0.1368 us |     0.0488 us | 10.19x faster |   0.11x |     1.4343 |         - |         - |    11.96 KB | 45.59x less |
| System.Text.Json       | Serialize   | Products  | 100   |     28.040 us |     0.6023 us |     0.2674 us |  9.42x faster |   0.13x |     3.3264 |         - |         - |    27.45 KB | 19.86x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Toon.Format            | Serialize   | Products  | 10000 | 51,143.991 us |   877.2911 us |   458.8402 us |      baseline |         |  7000.0000 | 3100.0000 | 1100.0000 | 54375.28 KB |             |
| ToonNet                | Serialize   | Products  | 10000 |  2,708.655 us |    16.5219 us |     8.6413 us | 18.88x faster |   0.17x |   101.5625 |  101.5625 |  101.5625 |   990.28 KB | 54.91x less |
| 'ToonNet (reflection)' | Serialize   | Products  | 10000 |  3,144.953 us |    14.1163 us |     6.2677 us | 16.26x faster |   0.14x |   101.5625 |  101.5625 |  101.5625 |   989.78 KB | 54.94x less |
| 'ToonNet (type info)'  | Serialize   | Products  | 10000 |  2,713.130 us |    39.8698 us |    17.7024 us | 18.85x faster |   0.20x |   101.5625 |  101.5625 |  101.5625 |   990.42 KB | 54.90x less |
| System.Text.Json       | Serialize   | Products  | 10000 |  2,974.211 us |    41.2337 us |    21.5660 us | 17.20x faster |   0.19x |   273.4375 |  273.4375 |  273.4375 |  2767.36 KB | 19.65x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Toon.Format            | Serialize   | Servers   | 100   |    107.720 us |     3.0145 us |     1.3385 us |      baseline |         |    34.1797 |    7.8125 |         - |   281.51 KB |             |
| ToonNet                | Serialize   | Servers   | 100   |      8.890 us |     0.1294 us |     0.0574 us | 12.12x faster |   0.16x |     1.3123 |         - |         - |    10.82 KB | 26.02x less |
| 'ToonNet (reflection)' | Serialize   | Servers   | 100   |      9.900 us |     0.0954 us |     0.0499 us | 10.88x faster |   0.14x |     1.2665 |         - |         - |    10.46 KB | 26.91x less |
| 'ToonNet (type info)'  | Serialize   | Servers   | 100   |      8.784 us |     0.0795 us |     0.0353 us | 12.26x faster |   0.15x |     1.3275 |         - |         - |    10.95 KB | 25.72x less |
| System.Text.Json       | Serialize   | Servers   | 100   |      8.904 us |     0.1871 us |     0.0831 us | 12.10x faster |   0.18x |     1.8768 |         - |         - |    15.37 KB | 18.32x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Toon.Format            | Serialize   | Servers   | 10000 | 23,901.553 us |   166.6329 us |    87.1522 us |      baseline |         |  3812.5000 | 1593.7500 |  562.5000 | 28252.61 KB |             |
| ToonNet                | Serialize   | Servers   | 10000 |    960.351 us |     4.7085 us |     2.0906 us | 24.89x faster |   0.10x |   147.4609 |  147.4609 |  147.4609 |  1025.93 KB | 27.54x less |
| 'ToonNet (reflection)' | Serialize   | Servers   | 10000 |  1,018.234 us |     5.6293 us |     2.9443 us | 23.47x faster |   0.10x |   146.4844 |  146.4844 |  146.4844 |  1025.86 KB | 27.54x less |
| 'ToonNet (type info)'  | Serialize   | Servers   | 10000 |    938.395 us |    27.5541 us |    14.4113 us | 25.48x faster |   0.38x |   146.4844 |  146.4844 |  146.4844 |   1026.1 KB | 27.53x less |
| System.Text.Json       | Serialize   | Servers   | 10000 |    945.775 us |    12.8539 us |     6.7228 us | 25.27x faster |   0.19x |   247.0703 |  247.0703 |  247.0703 |  1573.04 KB | 17.96x less |


## UTF-8 input and output

ToonNet through a string (`Encoding.UTF8.GetString` then `Deserialize`, `Serialize` then `Encoding.UTF8.GetBytes`)
against its UTF-8 overloads (`Deserialize(ReadOnlySpan<byte>)`, `SerializeToUtf8Bytes`, `Serialize` to a reused
`ArrayBufferWriter<byte>`), with System.Text.Json's UTF-8 methods for reference. Ratios are relative to going through a
string.

| Method                           | Categories  | Data      | Count | Mean         | Error       | StdDev     | Ratio        | RatioSD | Gen0     | Gen1     | Gen2     | Allocated  | Alloc Ratio     |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 'ToonNet via string'             | Deserialize | Employees | 100   |    51.840 us |   0.1503 us |  0.0786 us |     baseline |         |   5.3711 |   0.5493 |        - |    45096 B |                 |
| 'ToonNet UTF-8'                  | Deserialize | Employees | 100   |    48.677 us |   0.3062 us |  0.1360 us | 1.06x faster |   0.00x |   3.3569 |   0.2441 |        - |    28296 B |      1.59x less |
| 'System.Text.Json UTF-8'         | Deserialize | Employees | 100   |    32.593 us |   0.2697 us |  0.1411 us | 1.59x faster |   0.01x |   3.0518 |   0.1831 |        - |    25784 B |      1.75x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 'ToonNet via string'             | Deserialize | Employees | 10000 | 5,848.638 us |  55.1402 us | 28.8394 us |     baseline |         | 414.0625 | 265.6250 | 132.8125 |  4188532 B |                 |
| 'ToonNet UTF-8'                  | Deserialize | Employees | 10000 | 5,050.241 us |  65.0625 us | 34.0290 us | 1.16x faster |   0.01x | 281.2500 | 140.6250 |        - |  2404296 B |      1.74x less |
| 'System.Text.Json UTF-8'         | Deserialize | Employees | 10000 | 4,158.196 us |  32.3269 us | 16.9076 us | 1.41x faster |   0.01x | 328.1250 | 203.1250 |  78.1250 |  2583182 B |      1.62x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 'ToonNet via string'             | Deserialize | Orders    | 100   |    83.850 us |   2.0628 us |  1.0789 us |     baseline |         |  12.6953 |   2.5635 |        - |   107072 B |                 |
| 'ToonNet UTF-8'                  | Deserialize | Orders    | 100   |    81.485 us |   1.0343 us |  0.4592 us | 1.03x faster |   0.01x |   7.0801 |   0.8545 |        - |    60224 B |      1.78x less |
| 'System.Text.Json UTF-8'         | Deserialize | Orders    | 100   |    50.307 us |   1.0199 us |  0.5335 us | 1.67x faster |   0.03x |   7.1411 |   0.9766 |        - |    60184 B |      1.78x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 'ToonNet via string'             | Deserialize | Orders    | 10000 | 9,614.015 us |  38.6588 us | 13.7861 us |     baseline |         | 781.2500 | 437.5000 | 109.3750 | 10500484 B |                 |
| 'ToonNet UTF-8'                  | Deserialize | Orders    | 10000 | 8,989.046 us | 191.6603 us | 85.0984 us | 1.07x faster |   0.01x | 671.8750 | 328.1250 |        - |  5683424 B |      1.85x less |
| 'System.Text.Json UTF-8'         | Deserialize | Orders    | 10000 | 8,295.957 us |  64.3980 us | 28.5931 us | 1.16x faster |   0.00x | 812.5000 | 500.0000 | 187.5000 |  6023248 B |      1.74x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 'ToonNet via string'             | Deserialize | Products  | 100   |    44.773 us |   0.5372 us |  0.2810 us |     baseline |         |   5.8594 |   0.6104 |        - |    49120 B |                 |
| 'ToonNet UTF-8'                  | Deserialize | Products  | 100   |    43.772 us |   0.4547 us |  0.2019 us | 1.02x faster |   0.01x |   4.6997 |   0.3662 |        - |    39376 B |      1.25x less |
| 'System.Text.Json UTF-8'         | Deserialize | Products  | 100   |    41.620 us |   0.4720 us |  0.2469 us | 1.08x faster |   0.01x |   4.3945 |   0.3662 |        - |    36984 B |      1.33x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 'ToonNet via string'             | Deserialize | Products  | 10000 | 5,091.160 us |  96.0497 us | 42.6467 us |     baseline |         | 523.4375 | 312.5000 | 101.5625 |  4535167 B |                 |
| 'ToonNet UTF-8'                  | Deserialize | Products  | 10000 | 4,785.851 us | 144.0354 us | 75.3333 us | 1.06x faster |   0.02x | 414.0625 | 203.1250 |        - |  3524176 B |      1.29x less |
| 'System.Text.Json UTF-8'         | Deserialize | Products  | 10000 | 6,307.057 us | 153.1992 us | 80.1261 us | 1.24x slower |   0.02x | 484.3750 | 218.7500 |  78.1250 |  3703017 B |      1.22x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 'ToonNet via string'             | Deserialize | Servers   | 100   |    22.304 us |   0.5405 us |  0.2827 us |     baseline |         |   5.3711 |   0.4883 |        - |    45120 B |                 |
| 'ToonNet UTF-8'                  | Deserialize | Servers   | 100   |    21.588 us |   0.1385 us |  0.0615 us | 1.03x faster |   0.01x |   4.3945 |   0.3662 |        - |    36832 B |      1.23x less |
| 'System.Text.Json UTF-8'         | Deserialize | Servers   | 100   |    17.245 us |   0.2433 us |  0.1080 us | 1.29x faster |   0.02x |   3.1738 |   0.2441 |        - |    26664 B |      1.69x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 'ToonNet via string'             | Deserialize | Servers   | 10000 | 3,656.148 us |  45.2286 us | 20.0818 us |     baseline |         | 496.0938 | 367.1875 | 253.9063 |  4176506 B |                 |
| 'ToonNet UTF-8'                  | Deserialize | Servers   | 10000 | 3,655.765 us |  41.3499 us | 18.3596 us | 1.00x faster |   0.01x | 406.2500 | 257.8125 | 160.1563 |  3288190 B |      1.27x less |
| 'System.Text.Json UTF-8'         | Deserialize | Servers   | 10000 | 2,791.414 us |  51.4525 us | 26.9106 us | 1.31x faster |   0.01x | 335.9375 | 214.8438 | 128.9063 |  2612750 B |      1.60x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 'ToonNet via string'             | Serialize   | Employees | 100   |    19.200 us |   0.1032 us |  0.0368 us |     baseline |         |   3.0518 |   0.1526 |        - |    25760 B |                 |
| 'ToonNet SerializeToUtf8Bytes'   | Serialize   | Employees | 100   |    18.540 us |   0.1169 us |  0.0611 us | 1.04x faster |   0.00x |   1.0681 |   0.0305 |        - |     9008 B |      2.86x less |
| 'ToonNet IBufferWriter (reused)' | Serialize   | Employees | 100   |    18.101 us |   0.1399 us |  0.0732 us | 1.06x faster |   0.00x |   0.0610 |        - |        - |      592 B |     43.51x less |
| 'System.Text.Json UTF-8'         | Serialize   | Employees | 100   |    19.947 us |   0.2093 us |  0.1095 us | 1.04x slower |   0.01x |   2.0447 |        - |        - |    17160 B |      1.50x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 'ToonNet via string'             | Serialize   | Employees | 10000 | 2,065.787 us |   8.4094 us |  2.9989 us |     baseline |         | 144.5313 | 144.5313 | 144.5313 |  2677574 B |                 |
| 'ToonNet SerializeToUtf8Bytes'   | Serialize   | Employees | 10000 | 1,950.102 us |   5.3622 us |  2.3809 us | 1.06x faster |   0.00x |  95.7031 |  95.7031 |  95.7031 |   893711 B |     2.996x less |
| 'ToonNet IBufferWriter (reused)' | Serialize   | Employees | 10000 | 1,836.660 us |   4.7930 us |  2.5068 us | 1.12x faster |   0.00x |        - |        - |        - |      864 B | 3,099.044x less |
| 'System.Text.Json UTF-8'         | Serialize   | Employees | 10000 | 2,101.234 us |  18.9374 us |  9.9046 us | 1.02x slower |   0.00x | 167.9688 | 167.9688 | 167.9688 |  1743837 B |     1.535x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 'ToonNet via string'             | Serialize   | Orders    | 100   |    27.667 us |   0.0670 us |  0.0350 us |     baseline |         |   8.4229 |   0.7019 |        - |    70816 B |                 |
| 'ToonNet SerializeToUtf8Bytes'   | Serialize   | Orders    | 100   |    26.376 us |   0.2346 us |  0.1042 us | 1.05x faster |   0.00x |   2.8381 |   0.2136 |        - |    24016 B |     2.949x less |
| 'ToonNet IBufferWriter (reused)' | Serialize   | Orders    | 100   |    25.262 us |   0.1384 us |  0.0615 us | 1.10x faster |   0.00x |   0.0610 |        - |        - |      576 B |   122.944x less |
| 'System.Text.Json UTF-8'         | Serialize   | Orders    | 100   |    25.471 us |   0.2238 us |  0.0994 us | 1.09x faster |   0.00x |   2.5024 |        - |        - |    21144 B |     3.349x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 'ToonNet via string'             | Serialize   | Orders    | 10000 | 3,197.737 us |  22.6825 us | 10.0712 us |     baseline |         | 156.2500 | 156.2500 | 156.2500 |  7226272 B |                 |
| 'ToonNet SerializeToUtf8Bytes'   | Serialize   | Orders    | 10000 | 2,771.496 us |   8.1271 us |  2.8982 us | 1.15x faster |   0.00x | 113.2813 | 113.2813 | 113.2813 |  2409890 B |     2.999x less |
| 'ToonNet IBufferWriter (reused)' | Serialize   | Orders    | 10000 | 2,535.961 us |  21.3513 us | 11.1672 us | 1.26x faster |   0.01x |        - |        - |        - |      728 B | 9,926.198x less |
| 'System.Text.Json UTF-8'         | Serialize   | Orders    | 10000 | 2,733.751 us |  55.3533 us | 24.5772 us | 1.17x faster |   0.01x |  97.6563 |  97.6563 |  97.6563 |  2149389 B |     3.362x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 'ToonNet via string'             | Serialize   | Products  | 100   |    26.510 us |   0.1285 us |  0.0571 us |     baseline |         |   2.0447 |   0.0305 |        - |    17136 B |                 |
| 'ToonNet SerializeToUtf8Bytes'   | Serialize   | Products  | 100   |    26.147 us |   0.0750 us |  0.0392 us | 1.01x faster |   0.00x |   0.8850 |        - |        - |     7440 B |      2.30x less |
| 'ToonNet IBufferWriter (reused)' | Serialize   | Products  | 100   |    25.742 us |   0.1248 us |  0.0554 us | 1.03x faster |   0.00x |   0.2747 |        - |        - |     2552 B |      6.71x less |
| 'System.Text.Json UTF-8'         | Serialize   | Products  | 100   |    26.912 us |   0.1955 us |  0.0697 us | 1.02x slower |   0.00x |   1.6785 |        - |        - |    14232 B |      1.20x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 'ToonNet via string'             | Serialize   | Products  | 10000 | 2,783.758 us |  10.4797 us |  3.7371 us |     baseline |         | 175.7813 | 175.7813 | 175.7813 |  1520088 B |                 |
| 'ToonNet SerializeToUtf8Bytes'   | Serialize   | Products  | 10000 | 2,683.470 us |  33.4414 us | 14.8482 us | 1.04x faster |   0.01x |  58.5938 |  58.5938 |  58.5938 |   508470 B |     2.990x less |
| 'ToonNet IBufferWriter (reused)' | Serialize   | Products  | 10000 | 2,585.143 us |   9.3458 us |  3.3328 us | 1.08x faster |   0.00x |        - |        - |        - |     2672 B |   568.895x less |
| 'System.Text.Json UTF-8'         | Serialize   | Products  | 10000 | 2,863.636 us |  30.2520 us | 13.4321 us | 1.03x slower |   0.00x | 187.5000 | 187.5000 | 187.5000 |  1417454 B |     1.072x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 'ToonNet via string'             | Serialize   | Servers   | 100   |     9.242 us |   0.0451 us |  0.0200 us |     baseline |         |   1.8311 |   0.0305 |        - |    15368 B |                 |
| 'ToonNet SerializeToUtf8Bytes'   | Serialize   | Servers   | 100   |     8.859 us |   0.0302 us |  0.0158 us | 1.04x faster |   0.00x |   0.8392 |        - |        - |     7128 B |      2.16x less |
| 'ToonNet IBufferWriter (reused)' | Serialize   | Servers   | 100   |     8.682 us |   0.0342 us |  0.0179 us | 1.06x faster |   0.00x |   0.3510 |        - |        - |     2968 B |      5.18x less |
| 'System.Text.Json UTF-8'         | Serialize   | Servers   | 100   |     8.144 us |   0.1513 us |  0.0672 us | 1.13x faster |   0.01x |   0.9460 |        - |        - |     8040 B |      1.91x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 'ToonNet via string'             | Serialize   | Servers   | 10000 |   995.895 us |   4.1988 us |  1.4973 us |     baseline |         | 253.9063 | 253.9063 | 253.9063 |  1495657 B |                 |
| 'ToonNet SerializeToUtf8Bytes'   | Serialize   | Servers   | 10000 |   944.685 us |   3.1762 us |  1.4102 us | 1.05x faster |   0.00x | 109.3750 | 109.3750 | 109.3750 |   606471 B |      2.47x less |
| 'ToonNet IBufferWriter (reused)' | Serialize   | Servers   | 10000 |   890.538 us |   2.6939 us |  1.4090 us | 1.12x faster |   0.00x |  33.2031 |  33.2031 |  33.2031 |   161777 B |      9.25x less |
| 'System.Text.Json UTF-8'         | Serialize   | Servers   | 10000 |   892.164 us |   2.8097 us |  1.2475 us | 1.12x faster |   0.00x | 163.0859 | 163.0859 | 163.0859 |   805844 B |      1.86x less |
