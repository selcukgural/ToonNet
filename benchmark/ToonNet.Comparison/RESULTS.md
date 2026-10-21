# ToonNet vs Toon.Format – benchmark results

Measured with `dotnet run -c Release -- --filter "*" --launchCount 1 --warmupCount 3 --iterationCount 8` in this
directory on 2026-10-21 (one run for both tables).

```
BenchmarkDotNet v0.15.8, macOS Tahoe 26.6.2 (25G83) [Darwin 25.6.0]
Apple M3 Max, 1 CPU, 16 logical and 16 physical cores
.NET SDK 10.0.105, .NET 10.0.5 (Arm64 RyuJIT armv8.0-a)
Toon.Format 1.0.0, ToonNet 4.0.1
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
| Toon.Format            | Deserialize | Employees | 100   |    140.181 us |     3.4425 us |     1.5285 us |      baseline |         |    42.9688 |   11.7188 |         - |   350.97 KB |             |
| ToonNet                | Deserialize | Employees | 100   |     48.565 us |     0.9051 us |     0.4019 us |  2.89x faster |   0.04x |     3.3569 |    0.2441 |         - |    27.52 KB | 12.75x less |
| 'ToonNet (reflection)' | Deserialize | Employees | 100   |     60.619 us |     0.6198 us |     0.2752 us |  2.31x faster |   0.03x |     3.3569 |    0.2441 |         - |    27.49 KB | 12.77x less |
| 'ToonNet (type info)'  | Deserialize | Employees | 100   |     46.908 us |     0.4629 us |     0.2055 us |  2.99x faster |   0.03x |     3.3569 |    0.2441 |         - |    27.65 KB | 12.69x less |
| System.Text.Json       | Deserialize | Employees | 100   |     33.247 us |     0.2032 us |     0.0724 us |  4.22x faster |   0.04x |     3.0518 |    0.1831 |         - |    25.18 KB | 13.94x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Toon.Format            | Deserialize | Employees | 10000 | 31,978.731 us | 1,763.4774 us |   922.3328 us |      baseline |         |  5000.0000 | 2281.2500 |  843.7500 |    35153 KB |             |
| ToonNet                | Deserialize | Employees | 10000 |  6,584.055 us |   110.5131 us |    49.0685 us |  4.86x faster |   0.14x |   281.2500 |  140.6250 |         - |  2347.84 KB | 14.97x less |
| 'ToonNet (reflection)' | Deserialize | Employees | 10000 |  5,796.912 us |    90.2901 us |    47.2235 us |  5.52x faster |   0.16x |   281.2500 |  140.6250 |         - |   2347.8 KB | 14.97x less |
| 'ToonNet (type info)'  | Deserialize | Employees | 10000 |  5,066.598 us |    65.5062 us |    29.0852 us |  6.31x faster |   0.18x |   281.2500 |  140.6250 |         - |  2347.96 KB | 14.97x less |
| System.Text.Json       | Deserialize | Employees | 10000 |  4,176.797 us |    31.3271 us |    13.9094 us |  7.66x faster |   0.21x |   328.1250 |  203.1250 |   78.1250 |  2522.73 KB | 13.93x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Toon.Format            | Deserialize | Orders    | 100   |    250.376 us |    10.6599 us |     5.5753 us |      baseline |         |    99.1211 |   38.5742 |         - |   809.82 KB |             |
| ToonNet                | Deserialize | Orders    | 100   |     82.335 us |     1.7676 us |     0.9245 us |  3.04x faster |   0.07x |     7.0801 |    0.9766 |         - |     58.7 KB | 13.80x less |
| 'ToonNet (reflection)' | Deserialize | Orders    | 100   |     97.854 us |     0.9354 us |     0.4153 us |  2.56x faster |   0.05x |     7.0801 |    0.9766 |         - |    58.67 KB | 13.80x less |
| 'ToonNet (type info)'  | Deserialize | Orders    | 100   |     77.394 us |     0.3076 us |     0.1366 us |  3.24x faster |   0.07x |     7.2021 |    0.9766 |         - |    58.83 KB | 13.77x less |
| System.Text.Json       | Deserialize | Orders    | 100   |     52.748 us |     1.2500 us |     0.5550 us |  4.75x faster |   0.11x |     7.1411 |    0.9766 |         - |    58.77 KB | 13.78x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Toon.Format            | Deserialize | Orders    | 10000 | 71,418.384 us | 2,370.0990 us | 1,239.6076 us |      baseline |         | 10571.4286 | 4857.1429 | 1571.4286 | 80186.46 KB |             |
| ToonNet                | Deserialize | Orders    | 10000 |  9,402.536 us |   381.4556 us |   169.3686 us |  7.60x faster |   0.18x |   671.8750 |  328.1250 |         - |  5550.11 KB | 14.45x less |
| 'ToonNet (reflection)' | Deserialize | Orders    | 10000 | 10,782.274 us |   371.3387 us |   164.8767 us |  6.62x faster |   0.14x |   671.8750 |  328.1250 |         - |  5550.08 KB | 14.45x less |
| 'ToonNet (type info)'  | Deserialize | Orders    | 10000 |  9,008.039 us |   215.2837 us |   112.5976 us |  7.93x faster |   0.16x |   671.8750 |  328.1250 |         - |  5550.23 KB | 14.45x less |
| System.Text.Json       | Deserialize | Orders    | 10000 |  8,727.634 us |   319.3482 us |   167.0253 us |  8.19x faster |   0.20x |   812.5000 |  500.0000 |  187.5000 |  5882.29 KB | 13.63x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Toon.Format            | Deserialize | Products  | 100   |    144.609 us |     1.2001 us |     0.4280 us |      baseline |         |    36.1328 |    9.7656 |         - |   295.87 KB |             |
| ToonNet                | Deserialize | Products  | 100   |     44.677 us |     1.1748 us |     0.6144 us |  3.24x faster |   0.04x |     4.6387 |    0.4272 |         - |    38.34 KB |  7.72x less |
| 'ToonNet (reflection)' | Deserialize | Products  | 100   |     54.575 us |     0.6641 us |     0.2948 us |  2.65x faster |   0.02x |     4.6387 |    0.4272 |         - |    38.31 KB |  7.72x less |
| 'ToonNet (type info)'  | Deserialize | Products  | 100   |     42.539 us |     0.4586 us |     0.2036 us |  3.40x faster |   0.02x |     4.6997 |    0.4883 |         - |    38.47 KB |  7.69x less |
| System.Text.Json       | Deserialize | Products  | 100   |     43.080 us |     0.7635 us |     0.3993 us |  3.36x faster |   0.03x |     4.3945 |    0.3662 |         - |    36.12 KB |  8.19x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Toon.Format            | Deserialize | Products  | 10000 | 33,285.815 us |   315.9005 us |   140.2618 us |      baseline |         |  4333.3333 | 2133.3333 |  866.6667 | 29471.35 KB |             |
| ToonNet                | Deserialize | Products  | 10000 |  4,907.850 us |   113.7035 us |    50.4851 us |  6.78x faster |   0.07x |   414.0625 |  203.1250 |         - |  3441.47 KB |  8.56x less |
| 'ToonNet (reflection)' | Deserialize | Products  | 10000 |  6,362.021 us |    72.7765 us |    32.3132 us |  5.23x faster |   0.03x |   414.0625 |  203.1250 |         - |  3441.44 KB |  8.56x less |
| 'ToonNet (type info)'  | Deserialize | Products  | 10000 |  4,656.940 us |    91.6152 us |    40.6777 us |  7.15x faster |   0.07x |   414.0625 |  203.1250 |         - |  3441.59 KB |  8.56x less |
| System.Text.Json       | Deserialize | Products  | 10000 |  6,308.576 us |   142.2504 us |    74.3997 us |  5.28x faster |   0.06x |   484.3750 |  218.7500 |   78.1250 |   3616.3 KB |  8.15x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Toon.Format            | Deserialize | Servers   | 100   |     65.535 us |     0.8495 us |     0.4443 us |      baseline |         |    26.8555 |    5.8594 |         - |    223.3 KB |             |
| ToonNet                | Deserialize | Servers   | 100   |     23.296 us |     0.0867 us |     0.0385 us |  2.81x faster |   0.02x |     4.3640 |    0.3967 |         - |    35.86 KB |  6.23x less |
| 'ToonNet (reflection)' | Deserialize | Servers   | 100   |     24.843 us |     0.0891 us |     0.0396 us |  2.64x faster |   0.02x |     4.3640 |    0.3967 |         - |    35.83 KB |  6.23x less |
| 'ToonNet (type info)'  | Deserialize | Servers   | 100   |     20.172 us |     0.6446 us |     0.3372 us |  3.25x faster |   0.06x |     4.3945 |    0.4272 |         - |    35.98 KB |  6.21x less |
| System.Text.Json       | Deserialize | Servers   | 100   |     17.710 us |     0.0801 us |     0.0356 us |  3.70x faster |   0.02x |     3.1738 |    0.2441 |         - |    26.04 KB |  8.58x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Toon.Format            | Deserialize | Servers   | 10000 | 15,266.043 us |   487.5740 us |   255.0107 us |      baseline |         |  2843.7500 | 1437.5000 |  531.2500 | 22392.84 KB |             |
| ToonNet                | Deserialize | Servers   | 10000 |  3,548.367 us |    82.0437 us |    42.9104 us |  4.30x faster |   0.08x |   402.3438 |  265.6250 |  160.1563 |   3211.1 KB |  6.97x less |
| 'ToonNet (reflection)' | Deserialize | Servers   | 10000 |  3,682.839 us |    92.0142 us |    32.8132 us |  4.15x faster |   0.07x |   394.5313 |  253.9063 |  160.1563 |   3211.1 KB |  6.97x less |
| 'ToonNet (type info)'  | Deserialize | Servers   | 10000 |  3,292.282 us |    43.7986 us |    19.4469 us |  4.64x faster |   0.08x |   398.4375 |  253.9063 |  156.2500 |  3211.23 KB |  6.97x less |
| System.Text.Json       | Deserialize | Servers   | 10000 |  2,751.443 us |    84.7924 us |    44.3481 us |  5.55x faster |   0.12x |   269.5313 |  152.3438 |   70.3125 |  2551.14 KB |  8.78x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Toon.Format            | Serialize   | Employees | 100   |    231.959 us |    14.0096 us |     7.3273 us |      baseline |         |    62.5000 |   27.3438 |         - |   525.56 KB |             |
| ToonNet                | Serialize   | Employees | 100   |     19.165 us |     0.4468 us |     0.2337 us | 12.11x faster |   0.39x |     2.0447 |         - |         - |    16.81 KB | 31.26x less |
| 'ToonNet (reflection)' | Serialize   | Employees | 100   |     25.203 us |     0.4054 us |     0.2120 us |  9.20x faster |   0.28x |     2.0447 |         - |         - |    16.81 KB | 31.26x less |
| 'ToonNet (type info)'  | Serialize   | Employees | 100   |     18.315 us |     0.1595 us |     0.0708 us | 12.67x faster |   0.38x |     2.0447 |         - |         - |    16.94 KB | 31.03x less |
| System.Text.Json       | Serialize   | Employees | 100   |     21.011 us |     0.1693 us |     0.0752 us | 11.04x faster |   0.33x |     4.0283 |         - |         - |    33.19 KB | 15.84x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Toon.Format            | Serialize   | Employees | 10000 | 46,100.247 us | 2,286.7138 us | 1,195.9955 us |      baseline |         |  6818.1818 | 3181.8182 | 1000.0000 | 53017.27 KB |             |
| ToonNet                | Serialize   | Employees | 10000 |  1,967.266 us |     8.3708 us |     3.7167 us | 23.43x faster |   0.58x |   152.3438 |  152.3438 |  152.3438 |  1744.23 KB | 30.40x less |
| 'ToonNet (reflection)' | Serialize   | Employees | 10000 |  2,650.822 us |    25.9083 us |    13.5506 us | 17.39x faster |   0.43x |   152.3438 |  152.3438 |  152.3438 |  1744.33 KB | 30.39x less |
| 'ToonNet (type info)'  | Serialize   | Employees | 10000 |  1,950.732 us |    19.0172 us |     8.4437 us | 23.63x faster |   0.59x |   160.1563 |  160.1563 |  160.1563 |  1744.48 KB | 30.39x less |
| System.Text.Json       | Serialize   | Employees | 10000 |  2,270.482 us |    64.3096 us |    33.6351 us | 20.31x faster |   0.57x |   242.1875 |  242.1875 |  242.1875 |  3404.76 KB | 15.57x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Toon.Format            | Serialize   | Orders    | 100   |    337.124 us |    15.8085 us |     8.2681 us |      baseline |         |   105.4688 |   35.1563 |         - |   876.35 KB |             |
| ToonNet                | Serialize   | Orders    | 100   |     25.886 us |     0.2261 us |     0.1183 us | 13.02x faster |   0.31x |     5.6152 |         - |         - |    46.14 KB | 18.99x less |
| 'ToonNet (reflection)' | Serialize   | Orders    | 100   |     38.959 us |     0.4640 us |     0.2427 us |  8.65x faster |   0.21x |     5.6152 |         - |         - |    46.14 KB | 18.99x less |
| 'ToonNet (type info)'  | Serialize   | Orders    | 100   |     25.978 us |     0.7546 us |     0.3947 us | 12.98x faster |   0.35x |     5.6458 |         - |         - |    46.27 KB | 18.94x less |
| System.Text.Json       | Serialize   | Orders    | 100   |     26.885 us |     0.7837 us |     0.3480 us | 12.54x faster |   0.33x |     4.9744 |         - |         - |    40.96 KB | 21.39x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Toon.Format            | Serialize   | Orders    | 10000 | 74,601.462 us | 1,767.3273 us |   924.3463 us |      baseline |         | 10333.3333 | 4000.0000 | 1000.0000 | 87005.79 KB |             |
| ToonNet                | Serialize   | Orders    | 10000 |  2,805.918 us |    82.3048 us |    43.0470 us | 26.59x faster |   0.49x |   156.2500 |  156.2500 |  156.2500 |  4705.62 KB | 18.49x less |
| 'ToonNet (reflection)' | Serialize   | Orders    | 10000 |  4,047.981 us |   101.2290 us |    44.9463 us | 18.43x faster |   0.29x |   140.6250 |  140.6250 |  140.6250 |  4705.45 KB | 18.49x less |
| 'ToonNet (type info)'  | Serialize   | Orders    | 10000 |  2,776.802 us |    33.3496 us |    14.8074 us | 26.87x faster |   0.34x |   156.2500 |  156.2500 |  156.2500 |  4705.75 KB | 18.49x less |
| System.Text.Json       | Serialize   | Orders    | 10000 |  3,028.047 us |    42.5144 us |    18.8767 us | 24.64x faster |   0.32x |   140.6250 |  140.6250 |  140.6250 |  4197.46 KB | 20.73x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Toon.Format            | Serialize   | Products  | 100   |    268.013 us |    15.2199 us |     7.9603 us |      baseline |         |    66.4063 |   21.4844 |         - |   545.25 KB |             |
| ToonNet                | Serialize   | Products  | 100   |     26.738 us |     0.5527 us |     0.2891 us | 10.02x faster |   0.30x |     1.4343 |         - |         - |    11.84 KB | 46.07x less |
| 'ToonNet (reflection)' | Serialize   | Products  | 100   |     38.204 us |     1.0784 us |     0.5640 us |  7.02x faster |   0.22x |     1.3428 |         - |         - |    11.13 KB | 48.98x less |
| 'ToonNet (type info)'  | Serialize   | Products  | 100   |     26.036 us |     0.3068 us |     0.1362 us | 10.29x faster |   0.29x |     1.4343 |         - |         - |    11.96 KB | 45.59x less |
| System.Text.Json       | Serialize   | Products  | 100   |     27.718 us |     0.6623 us |     0.3464 us |  9.67x faster |   0.29x |     3.3264 |         - |         - |    27.45 KB | 19.86x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Toon.Format            | Serialize   | Products  | 10000 | 53,160.204 us | 3,006.8860 us | 1,572.6595 us |      baseline |         |  6750.0000 | 3000.0000 |  750.0000 | 54374.38 KB |             |
| ToonNet                | Serialize   | Products  | 10000 |  2,772.841 us |    82.7606 us |    43.2854 us | 19.18x faster |   0.60x |   101.5625 |  101.5625 |  101.5625 |   990.28 KB | 54.91x less |
| 'ToonNet (reflection)' | Serialize   | Products  | 10000 |  3,190.748 us |    84.2149 us |    44.0460 us | 16.66x faster |   0.51x |   101.5625 |  101.5625 |  101.5625 |   989.79 KB | 54.94x less |
| 'ToonNet (type info)'  | Serialize   | Products  | 10000 |  2,726.135 us |    91.2924 us |    47.7477 us | 19.51x faster |   0.63x |   101.5625 |  101.5625 |  101.5625 |    990.4 KB | 54.90x less |
| System.Text.Json       | Serialize   | Products  | 10000 |  2,970.883 us |    92.5140 us |    48.3866 us | 17.90x faster |   0.57x |   250.0000 |  250.0000 |  250.0000 |  2766.87 KB | 19.65x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Toon.Format            | Serialize   | Servers   | 100   |    108.711 us |     2.3826 us |     1.2461 us |      baseline |         |    34.1797 |    7.8125 |         - |   281.51 KB |             |
| ToonNet                | Serialize   | Servers   | 100   |      9.254 us |     0.1887 us |     0.0838 us | 11.75x faster |   0.16x |     1.3123 |         - |         - |    10.82 KB | 26.02x less |
| 'ToonNet (reflection)' | Serialize   | Servers   | 100   |     10.070 us |     0.2685 us |     0.1405 us | 10.80x faster |   0.18x |     1.2665 |         - |         - |    10.46 KB | 26.91x less |
| 'ToonNet (type info)'  | Serialize   | Servers   | 100   |      9.101 us |     0.1392 us |     0.0618 us | 11.95x faster |   0.15x |     1.3275 |         - |         - |    10.95 KB | 25.72x less |
| System.Text.Json       | Serialize   | Servers   | 100   |      8.934 us |     0.2023 us |     0.1058 us | 12.17x faster |   0.19x |     1.8768 |         - |         - |    15.37 KB | 18.32x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Toon.Format            | Serialize   | Servers   | 10000 | 23,587.862 us |   706.5516 us |   369.5401 us |      baseline |         |  3812.5000 | 1593.7500 |  562.5000 | 28252.75 KB |             |
| ToonNet                | Serialize   | Servers   | 10000 |    967.242 us |    12.9425 us |     5.7466 us | 24.39x faster |   0.38x |   146.4844 |  146.4844 |  146.4844 |  1025.92 KB | 27.54x less |
| 'ToonNet (reflection)' | Serialize   | Servers   | 10000 |  1,026.973 us |     4.8722 us |     2.1633 us | 22.97x faster |   0.34x |   146.4844 |  146.4844 |  146.4844 |  1025.86 KB | 27.54x less |
| 'ToonNet (type info)'  | Serialize   | Servers   | 10000 |    960.697 us |     6.5652 us |     3.4337 us | 24.55x faster |   0.37x |   147.4609 |  147.4609 |  147.4609 |   1026.1 KB | 27.53x less |
| System.Text.Json       | Serialize   | Servers   | 10000 |    939.909 us |    23.0032 us |    12.0311 us | 25.10x faster |   0.48x |   243.1641 |  243.1641 |  243.1641 |     1573 KB | 17.96x less |


## UTF-8 input and output

ToonNet through a string (`Encoding.UTF8.GetString` then `Deserialize`, `Serialize` then `Encoding.UTF8.GetBytes`)
against its UTF-8 overloads (`Deserialize(ReadOnlySpan<byte>)`, `SerializeToUtf8Bytes`, `Serialize` to a reused
`ArrayBufferWriter<byte>`), with System.Text.Json's UTF-8 methods for reference. Ratios are relative to going through a
string.

| Method                           | Categories  | Data      | Count | Mean         | Error       | StdDev      | Ratio        | RatioSD | Gen0     | Gen1     | Gen2     | Allocated  | Alloc Ratio     |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 'ToonNet via string'             | Deserialize | Employees | 100   |    51.861 us |   0.7861 us |   0.3490 us |     baseline |         |   5.3711 |   0.6104 |        - |    45112 B |                 |
| 'ToonNet UTF-8'                  | Deserialize | Employees | 100   |    51.482 us |   0.5125 us |   0.2276 us | 1.01x faster |   0.01x |   3.3569 |   0.2441 |        - |    28312 B |      1.59x less |
| 'System.Text.Json UTF-8'         | Deserialize | Employees | 100   |    32.936 us |   0.7355 us |   0.3847 us | 1.57x faster |   0.02x |   3.0518 |   0.1831 |        - |    25784 B |      1.75x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 'ToonNet via string'             | Deserialize | Employees | 10000 | 5,993.392 us | 161.5627 us |  84.5004 us |     baseline |         | 421.8750 | 273.4375 | 140.6250 |  4188565 B |                 |
| 'ToonNet UTF-8'                  | Deserialize | Employees | 10000 | 5,368.751 us | 188.0962 us |  98.3780 us | 1.12x faster |   0.02x | 281.2500 | 140.6250 |        - |  2404312 B |      1.74x less |
| 'System.Text.Json UTF-8'         | Deserialize | Employees | 10000 | 4,105.126 us |  80.2014 us |  35.6099 us | 1.46x faster |   0.02x | 328.1250 | 203.1250 |  78.1250 |  2583093 B |      1.62x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 'ToonNet via string'             | Deserialize | Orders    | 100   |    83.670 us |   0.9121 us |   0.4050 us |     baseline |         |  12.6953 |   2.6855 |        - |   107088 B |                 |
| 'ToonNet UTF-8'                  | Deserialize | Orders    | 100   |    80.652 us |   2.0176 us |   1.0552 us | 1.04x faster |   0.01x |   7.2021 |   0.9766 |        - |    60240 B |      1.78x less |
| 'System.Text.Json UTF-8'         | Deserialize | Orders    | 100   |    51.426 us |   0.8643 us |   0.4521 us | 1.63x faster |   0.02x |   7.1411 |   0.9766 |        - |    60184 B |      1.78x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 'ToonNet via string'             | Deserialize | Orders    | 10000 | 9,816.546 us | 288.3581 us | 128.0328 us |     baseline |         | 781.2500 | 437.5000 | 109.3750 | 10500498 B |                 |
| 'ToonNet UTF-8'                  | Deserialize | Orders    | 10000 | 9,317.580 us | 255.7859 us | 133.7810 us | 1.05x faster |   0.02x | 671.8750 | 328.1250 |        - |  5683440 B |      1.85x less |
| 'System.Text.Json UTF-8'         | Deserialize | Orders    | 10000 | 8,422.786 us |  52.5635 us |  23.3385 us | 1.17x faster |   0.01x | 812.5000 | 500.0000 | 187.5000 |  6023258 B |      1.74x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 'ToonNet via string'             | Deserialize | Products  | 100   |    46.022 us |   1.1302 us |   0.5911 us |     baseline |         |   5.8594 |   0.6104 |        - |    49136 B |                 |
| 'ToonNet UTF-8'                  | Deserialize | Products  | 100   |    44.952 us |   0.6607 us |   0.3455 us | 1.02x faster |   0.01x |   4.6997 |   0.4883 |        - |    39392 B |      1.25x less |
| 'System.Text.Json UTF-8'         | Deserialize | Products  | 100   |    43.024 us |   0.8493 us |   0.3771 us | 1.07x faster |   0.02x |   4.3945 |   0.3662 |        - |    36984 B |      1.33x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 'ToonNet via string'             | Deserialize | Products  | 10000 | 5,579.730 us | 121.3812 us |  53.8940 us |     baseline |         | 523.4375 | 312.5000 | 101.5625 |  4535184 B |                 |
| 'ToonNet UTF-8'                  | Deserialize | Products  | 10000 | 4,869.381 us | 152.8633 us |  79.9504 us | 1.15x faster |   0.02x | 414.0625 | 203.1250 |        - |  3524192 B |      1.29x less |
| 'System.Text.Json UTF-8'         | Deserialize | Products  | 10000 | 6,313.818 us | 175.8585 us |  91.9774 us | 1.13x slower |   0.02x | 484.3750 | 218.7500 |  78.1250 |  3703017 B |      1.22x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 'ToonNet via string'             | Deserialize | Servers   | 100   |    22.928 us |   0.5697 us |   0.2979 us |     baseline |         |   5.3711 |   0.5798 |        - |    45136 B |                 |
| 'ToonNet UTF-8'                  | Deserialize | Servers   | 100   |    22.681 us |   1.0658 us |   0.5574 us | 1.01x faster |   0.03x |   4.3945 |   0.4272 |        - |    36848 B |      1.22x less |
| 'System.Text.Json UTF-8'         | Deserialize | Servers   | 100   |    17.346 us |   0.2703 us |   0.1200 us | 1.32x faster |   0.02x |   3.1738 |   0.2441 |        - |    26664 B |      1.69x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 'ToonNet via string'             | Deserialize | Servers   | 10000 | 3,730.632 us |  74.4956 us |  38.9626 us |     baseline |         | 484.3750 | 343.7500 | 242.1875 |  4176453 B |                 |
| 'ToonNet UTF-8'                  | Deserialize | Servers   | 10000 | 3,629.313 us |  43.6604 us |  19.3855 us | 1.03x faster |   0.01x | 406.2500 | 257.8125 | 160.1563 |  3288201 B |      1.27x less |
| 'System.Text.Json UTF-8'         | Deserialize | Servers   | 10000 | 2,765.169 us |  27.8788 us |  12.3783 us | 1.35x faster |   0.01x | 335.9375 | 214.8438 | 128.9063 |  2612687 B |      1.60x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 'ToonNet via string'             | Serialize   | Employees | 100   |    19.238 us |   0.1186 us |   0.0527 us |     baseline |         |   3.0518 |   0.1526 |        - |    25760 B |                 |
| 'ToonNet SerializeToUtf8Bytes'   | Serialize   | Employees | 100   |    19.424 us |   0.7770 us |   0.4064 us | 1.01x slower |   0.02x |   1.0681 |   0.0305 |        - |     9008 B |      2.86x less |
| 'ToonNet IBufferWriter (reused)' | Serialize   | Employees | 100   |    18.258 us |   0.4088 us |   0.2138 us | 1.05x faster |   0.01x |   0.0610 |        - |        - |      592 B |     43.51x less |
| 'System.Text.Json UTF-8'         | Serialize   | Employees | 100   |    20.394 us |   0.5720 us |   0.2992 us | 1.06x slower |   0.01x |   2.0447 |        - |        - |    17160 B |      1.50x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 'ToonNet via string'             | Serialize   | Employees | 10000 | 2,146.813 us |  47.8395 us |  25.0210 us |     baseline |         | 144.5313 | 144.5313 | 144.5313 |  2677574 B |                 |
| 'ToonNet SerializeToUtf8Bytes'   | Serialize   | Employees | 10000 | 1,973.099 us |  31.1956 us |  16.3159 us | 1.09x faster |   0.01x |  95.7031 |  95.7031 |  95.7031 |   893711 B |     2.996x less |
| 'ToonNet IBufferWriter (reused)' | Serialize   | Employees | 10000 | 1,858.278 us |  32.1700 us |  16.8255 us | 1.16x faster |   0.02x |        - |        - |        - |      864 B | 3,099.044x less |
| 'System.Text.Json UTF-8'         | Serialize   | Employees | 10000 | 2,151.720 us |  24.6249 us |  10.9336 us | 1.00x slower |   0.01x | 125.0000 | 125.0000 | 125.0000 |  1743320 B |     1.536x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 'ToonNet via string'             | Serialize   | Orders    | 100   |    27.693 us |   0.3144 us |   0.1396 us |     baseline |         |   8.4229 |   0.7019 |        - |    70816 B |                 |
| 'ToonNet SerializeToUtf8Bytes'   | Serialize   | Orders    | 100   |    26.405 us |   0.2231 us |   0.1167 us | 1.05x faster |   0.01x |   2.8381 |   0.2136 |        - |    24016 B |     2.949x less |
| 'ToonNet IBufferWriter (reused)' | Serialize   | Orders    | 100   |    25.246 us |   0.0825 us |   0.0366 us | 1.10x faster |   0.01x |   0.0610 |        - |        - |      576 B |   122.944x less |
| 'System.Text.Json UTF-8'         | Serialize   | Orders    | 100   |    25.656 us |   0.2677 us |   0.1400 us | 1.08x faster |   0.01x |   2.5024 |        - |        - |    21144 B |     3.349x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 'ToonNet via string'             | Serialize   | Orders    | 10000 | 3,219.118 us |  71.8579 us |  37.5831 us |     baseline |         | 156.2500 | 156.2500 | 156.2500 |  7226277 B |                 |
| 'ToonNet SerializeToUtf8Bytes'   | Serialize   | Orders    | 10000 | 2,785.542 us |  39.0007 us |  17.3166 us | 1.16x faster |   0.01x | 113.2813 | 113.2813 | 113.2813 |  2409890 B |     2.999x less |
| 'ToonNet IBufferWriter (reused)' | Serialize   | Orders    | 10000 | 2,530.341 us |  19.7168 us |   8.7544 us | 1.27x faster |   0.01x |        - |        - |        - |      728 B | 9,926.205x less |
| 'System.Text.Json UTF-8'         | Serialize   | Orders    | 10000 | 2,767.708 us |  42.1309 us |  22.0353 us | 1.16x faster |   0.02x |  97.6563 |  97.6563 |  97.6563 |  2149389 B |     3.362x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 'ToonNet via string'             | Serialize   | Products  | 100   |    29.805 us |   0.3114 us |   0.1383 us |     baseline |         |   2.0447 |   0.0305 |        - |    17136 B |                 |
| 'ToonNet SerializeToUtf8Bytes'   | Serialize   | Products  | 100   |    26.260 us |   0.2086 us |   0.0926 us | 1.14x faster |   0.01x |   0.8850 |        - |        - |     7440 B |      2.30x less |
| 'ToonNet IBufferWriter (reused)' | Serialize   | Products  | 100   |    26.181 us |   0.2888 us |   0.1282 us | 1.14x faster |   0.01x |   0.2747 |        - |        - |     2552 B |      6.71x less |
| 'System.Text.Json UTF-8'         | Serialize   | Products  | 100   |    26.271 us |   0.2655 us |   0.1179 us | 1.13x faster |   0.01x |   1.6785 |        - |        - |    14232 B |      1.20x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 'ToonNet via string'             | Serialize   | Products  | 10000 | 2,787.790 us |  12.3188 us |   4.3930 us |     baseline |         | 175.7813 | 175.7813 | 175.7813 |  1520083 B |                 |
| 'ToonNet SerializeToUtf8Bytes'   | Serialize   | Products  | 10000 | 2,704.966 us |  28.8757 us |  15.1025 us | 1.03x faster |   0.01x |  58.5938 |  58.5938 |  58.5938 |   508483 B |     2.989x less |
| 'ToonNet IBufferWriter (reused)' | Serialize   | Products  | 10000 | 2,630.650 us |  22.9959 us |  10.2103 us | 1.06x faster |   0.00x |        - |        - |        - |     2672 B |   568.893x less |
| 'System.Text.Json UTF-8'         | Serialize   | Products  | 10000 | 2,871.070 us |  25.4141 us |  13.2921 us | 1.03x slower |   0.00x | 187.5000 | 187.5000 | 187.5000 |  1417397 B |     1.072x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 'ToonNet via string'             | Serialize   | Servers   | 100   |     9.462 us |   0.0342 us |   0.0179 us |     baseline |         |   1.8311 |   0.0305 |        - |    15368 B |                 |
| 'ToonNet SerializeToUtf8Bytes'   | Serialize   | Servers   | 100   |     9.138 us |   0.0881 us |   0.0391 us | 1.04x faster |   0.00x |   0.8392 |        - |        - |     7128 B |      2.16x less |
| 'ToonNet IBufferWriter (reused)' | Serialize   | Servers   | 100   |     9.066 us |   0.0620 us |   0.0221 us | 1.04x faster |   0.00x |   0.3510 |        - |        - |     2968 B |      5.18x less |
| 'System.Text.Json UTF-8'         | Serialize   | Servers   | 100   |     8.193 us |   0.0465 us |   0.0206 us | 1.15x faster |   0.00x |   0.9460 |        - |        - |     8040 B |      1.91x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 'ToonNet via string'             | Serialize   | Servers   | 10000 | 1,027.439 us |  17.2350 us |   9.0142 us |     baseline |         | 255.8594 | 255.8594 | 255.8594 |  1495713 B |                 |
| 'ToonNet SerializeToUtf8Bytes'   | Serialize   | Servers   | 10000 |   949.107 us |   5.2693 us |   2.3396 us | 1.08x faster |   0.01x | 109.3750 | 109.3750 | 109.3750 |   606482 B |      2.47x less |
| 'ToonNet IBufferWriter (reused)' | Serialize   | Servers   | 10000 |   920.584 us |  21.8023 us |  11.4030 us | 1.12x faster |   0.02x |  33.2031 |  33.2031 |  33.2031 |   161777 B |      9.25x less |
| 'System.Text.Json UTF-8'         | Serialize   | Servers   | 10000 |   933.917 us |  11.5483 us |   5.1275 us | 1.10x faster |   0.01x | 163.0859 | 163.0859 | 163.0859 |   805834 B |      1.86x less |
