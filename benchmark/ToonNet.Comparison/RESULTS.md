# ToonNet vs Toon.Format – benchmark results

Measured with `dotnet run -c Release -- --filter "*" --launchCount 1 --warmupCount 3 --iterationCount 8` in this
directory on 2026-10-14 (the UTF-8 table with `--filter "*Utf8Benchmarks*"`).

```
BenchmarkDotNet v0.15.8, macOS Tahoe 26.6.2 (25G83) [Darwin 25.6.0]
Apple M3 Max, 1 CPU, 16 logical and 16 physical cores
.NET SDK 10.0.105, .NET 10.0.5 (Arm64 RyuJIT armv8.0-a)
Toon.Format 1.0.0, ToonNet 3.1.0
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
| Toon.Format            | Deserialize | Employees | 100   |    137.257 us |     1.5091 us |     0.7893 us |      baseline |         |    42.9688 |   11.7188 |         - |   350.97 KB |             |
| ToonNet                | Deserialize | Employees | 100   |     45.813 us |     0.4409 us |     0.1957 us |  3.00x faster |   0.02x |     3.3569 |    0.0610 |         - |    27.51 KB | 12.76x less |
| 'ToonNet (reflection)' | Deserialize | Employees | 100   |     50.555 us |     0.1441 us |     0.0640 us |  2.72x faster |   0.02x |     3.3569 |    0.2441 |         - |    27.48 KB | 12.77x less |
| 'ToonNet (type info)'  | Deserialize | Employees | 100   |     44.481 us |     0.1583 us |     0.0703 us |  3.09x faster |   0.02x |     3.3569 |    0.2441 |         - |    27.63 KB | 12.70x less |
| System.Text.Json       | Deserialize | Employees | 100   |     33.232 us |     0.4364 us |     0.2283 us |  4.13x faster |   0.03x |     3.0518 |    0.1831 |         - |    25.18 KB | 13.94x less |
| Toon.Format            | Deserialize | Employees | 10000 | 31,440.310 us |   522.9282 us |   273.5015 us |      baseline |         |  5000.0000 | 2250.0000 |  812.5000 | 35152.92 KB |             |
| ToonNet                | Deserialize | Employees | 10000 |  5,329.918 us |    37.3702 us |    16.5926 us |  5.90x faster |   0.05x |   281.2500 |  140.6250 |         - |  2347.82 KB | 14.97x less |
| 'ToonNet (reflection)' | Deserialize | Employees | 10000 |  5,267.329 us |    41.9783 us |    18.6386 us |  5.97x faster |   0.05x |   281.2500 |  140.6250 |         - |  2347.79 KB | 14.97x less |
| 'ToonNet (type info)'  | Deserialize | Employees | 10000 |  4,687.449 us |    30.8309 us |    13.6891 us |  6.71x faster |   0.06x |   281.2500 |  140.6250 |         - |  2347.94 KB | 14.97x less |
| System.Text.Json       | Deserialize | Employees | 10000 |  4,193.275 us |    35.0085 us |    15.5440 us |  7.50x faster |   0.07x |   328.1250 |  203.1250 |   78.1250 |   2522.7 KB | 13.93x less |
| Toon.Format            | Deserialize | Orders    | 100   |    244.290 us |     2.2336 us |     1.1682 us |      baseline |         |    97.6563 |   37.1094 |         - |   809.82 KB |             |
| ToonNet                | Deserialize | Orders    | 100   |     80.736 us |     0.5177 us |     0.2299 us |  3.03x faster |   0.02x |     7.0801 |    0.9766 |         - |    58.69 KB | 13.80x less |
| 'ToonNet (reflection)' | Deserialize | Orders    | 100   |     94.049 us |     0.3026 us |     0.1343 us |  2.60x faster |   0.01x |     7.0801 |    0.9766 |         - |    58.66 KB | 13.81x less |
| 'ToonNet (type info)'  | Deserialize | Orders    | 100   |     79.527 us |     0.7816 us |     0.4088 us |  3.07x faster |   0.02x |     7.0801 |    0.9766 |         - |     58.8 KB | 13.77x less |
| System.Text.Json       | Deserialize | Orders    | 100   |     50.934 us |     0.4947 us |     0.2587 us |  4.80x faster |   0.03x |     7.1411 |    0.9766 |         - |    58.77 KB | 13.78x less |
| Toon.Format            | Deserialize | Orders    | 10000 | 73,994.978 us | 4,035.2208 us | 2,110.4985 us |      baseline |         | 10857.1429 | 5142.8571 | 1714.2857 | 80186.86 KB |             |
| ToonNet                | Deserialize | Orders    | 10000 |  9,575.106 us |    53.0266 us |    23.5441 us |  7.73x faster |   0.21x |   843.7500 |  421.8750 |         - |   6956.2 KB | 11.53x less |
| 'ToonNet (reflection)' | Deserialize | Orders    | 10000 | 10,481.345 us |   104.5657 us |    46.4278 us |  7.06x faster |   0.19x |   671.8750 |  328.1250 |         - |  5550.06 KB | 14.45x less |
| 'ToonNet (type info)'  | Deserialize | Orders    | 10000 |  9,205.753 us |   121.5021 us |    63.5480 us |  8.04x faster |   0.22x |   750.0000 |  375.0000 |         - |  6253.27 KB | 12.82x less |
| System.Text.Json       | Deserialize | Orders    | 10000 |  8,469.767 us |    55.3056 us |    24.5560 us |  8.74x faster |   0.24x |   812.5000 |  500.0000 |  187.5000 |   5882.3 KB | 13.63x less |
| Toon.Format            | Deserialize | Products  | 100   |    142.196 us |     1.0763 us |     0.4779 us |      baseline |         |    36.1328 |    9.7656 |         - |   295.87 KB |             |
| ToonNet                | Deserialize | Products  | 100   |     43.005 us |     0.2029 us |     0.0901 us |  3.31x faster |   0.01x |     4.6387 |    0.4272 |         - |    38.33 KB |  7.72x less |
| 'ToonNet (reflection)' | Deserialize | Products  | 100   |     51.505 us |     0.4767 us |     0.2117 us |  2.76x faster |   0.01x |     4.6387 |    0.4272 |         - |     38.3 KB |  7.73x less |
| 'ToonNet (type info)'  | Deserialize | Products  | 100   |     42.449 us |     0.4551 us |     0.2021 us |  3.35x faster |   0.02x |     4.6997 |    0.4272 |         - |    38.45 KB |  7.70x less |
| System.Text.Json       | Deserialize | Products  | 100   |     42.398 us |     0.2679 us |     0.1190 us |  3.35x faster |   0.01x |     4.3945 |    0.3662 |         - |    36.12 KB |  8.19x less |
| Toon.Format            | Deserialize | Products  | 10000 | 33,757.781 us |   289.5843 us |   128.5772 us |      baseline |         |  4333.3333 | 2133.3333 |  866.6667 | 29471.35 KB |             |
| ToonNet                | Deserialize | Products  | 10000 |  4,891.493 us |    19.1799 us |     8.5160 us |  6.90x faster |   0.03x |   414.0625 |  203.1250 |         - |  3441.45 KB |  8.56x less |
| 'ToonNet (reflection)' | Deserialize | Products  | 10000 |  5,569.150 us |    18.6830 us |     6.6625 us |  6.06x faster |   0.02x |   414.0625 |  203.1250 |         - |  3441.42 KB |  8.56x less |
| 'ToonNet (type info)'  | Deserialize | Products  | 10000 |  4,649.280 us |    49.8296 us |    22.1247 us |  7.26x faster |   0.04x |   414.0625 |  203.1250 |         - |  3441.57 KB |  8.56x less |
| System.Text.Json       | Deserialize | Products  | 10000 |  6,258.689 us |    70.3760 us |    36.8080 us |  5.39x faster |   0.04x |   484.3750 |  218.7500 |   78.1250 |   3616.3 KB |  8.15x less |
| Toon.Format            | Deserialize | Servers   | 100   |     67.961 us |     1.8119 us |     0.9477 us |      baseline |         |    26.8555 |    5.8594 |         - |    223.3 KB |             |
| ToonNet                | Deserialize | Servers   | 100   |     20.052 us |     0.1036 us |     0.0369 us |  3.39x faster |   0.05x |     4.3640 |    0.4272 |         - |    35.84 KB |  6.23x less |
| 'ToonNet (reflection)' | Deserialize | Servers   | 100   |     24.274 us |     0.3933 us |     0.1746 us |  2.80x faster |   0.04x |     4.3640 |    0.3357 |         - |    35.81 KB |  6.24x less |
| 'ToonNet (type info)'  | Deserialize | Servers   | 100   |     19.737 us |     0.1118 us |     0.0496 us |  3.44x faster |   0.05x |     4.3945 |    0.3967 |         - |    35.96 KB |  6.21x less |
| System.Text.Json       | Deserialize | Servers   | 100   |     17.573 us |     0.1085 us |     0.0482 us |  3.87x faster |   0.05x |     3.1738 |    0.2441 |         - |    26.04 KB |  8.58x less |
| Toon.Format            | Deserialize | Servers   | 10000 | 15,319.634 us |   212.1873 us |    94.2125 us |      baseline |         |  2843.7500 | 1437.5000 |  531.2500 | 22392.81 KB |             |
| ToonNet                | Deserialize | Servers   | 10000 |  3,392.660 us |    23.0643 us |    10.2407 us |  4.52x faster |   0.03x |   402.3438 |  265.6250 |  160.1563 |  3211.08 KB |  6.97x less |
| 'ToonNet (reflection)' | Deserialize | Servers   | 10000 |  3,768.631 us |   129.9390 us |    67.9606 us |  4.07x faster |   0.07x |   394.5313 |  253.9063 |  160.1563 |  3211.05 KB |  6.97x less |
| 'ToonNet (type info)'  | Deserialize | Servers   | 10000 |  3,277.009 us |    47.0566 us |    20.8934 us |  4.68x faster |   0.04x |   398.4375 |  261.7188 |  160.1563 |  3211.29 KB |  6.97x less |
| System.Text.Json       | Deserialize | Servers   | 10000 |  2,750.090 us |    73.7991 us |    32.7672 us |  5.57x faster |   0.07x |   269.5313 |  152.3438 |   70.3125 |   2551.2 KB |  8.78x less |
| Toon.Format            | Serialize   | Employees | 100   |    231.143 us |     2.4847 us |     1.1032 us |      baseline |         |    62.5000 |   27.3438 |         - |   525.56 KB |             |
| ToonNet                | Serialize   | Employees | 100   |     18.714 us |     0.9118 us |     0.4769 us | 12.36x faster |   0.30x |     2.0447 |         - |         - |    16.81 KB | 31.26x less |
| 'ToonNet (reflection)' | Serialize   | Employees | 100   |     25.224 us |     0.7992 us |     0.4180 us |  9.17x faster |   0.15x |     2.0447 |         - |         - |    16.81 KB | 31.26x less |
| 'ToonNet (type info)'  | Serialize   | Employees | 100   |     18.303 us |     0.6940 us |     0.3630 us | 12.63x faster |   0.24x |     2.0447 |         - |         - |    16.93 KB | 31.04x less |
| System.Text.Json       | Serialize   | Employees | 100   |     20.951 us |     0.3779 us |     0.1678 us | 11.03x faster |   0.10x |     4.0283 |         - |         - |    33.19 KB | 15.84x less |
| Toon.Format            | Serialize   | Employees | 10000 | 46,743.675 us | 2,356.5704 us | 1,232.5319 us |      baseline |         |  6818.1818 | 3181.8182 | 1000.0000 | 53017.24 KB |             |
| ToonNet                | Serialize   | Employees | 10000 |  1,990.964 us |    44.0255 us |    19.5476 us | 23.48x faster |   0.62x |   152.3438 |  152.3438 |  152.3438 |  1744.22 KB | 30.40x less |
| 'ToonNet (reflection)' | Serialize   | Employees | 10000 |  2,656.124 us |    15.8571 us |     7.0407 us | 17.60x faster |   0.44x |   152.3438 |  152.3438 |  152.3438 |  1744.27 KB | 30.40x less |
| 'ToonNet (type info)'  | Serialize   | Employees | 10000 |  1,964.229 us |     4.6999 us |     2.0868 us | 23.80x faster |   0.59x |   152.3438 |  152.3438 |  152.3438 |  1744.28 KB | 30.39x less |
| System.Text.Json       | Serialize   | Employees | 10000 |  2,268.954 us |    35.4757 us |    18.5545 us | 20.60x faster |   0.54x |   242.1875 |  242.1875 |  242.1875 |  3404.85 KB | 15.57x less |
| Toon.Format            | Serialize   | Orders    | 100   |    326.793 us |     2.9354 us |     1.5353 us |      baseline |         |   105.4688 |   35.1563 |         - |   876.35 KB |             |
| ToonNet                | Serialize   | Orders    | 100   |     25.916 us |     0.2070 us |     0.0919 us | 12.61x faster |   0.07x |     5.6152 |         - |         - |    46.14 KB | 18.99x less |
| 'ToonNet (reflection)' | Serialize   | Orders    | 100   |     44.361 us |     0.5020 us |     0.2626 us |  7.37x faster |   0.05x |     6.0425 |         - |         - |    50.05 KB | 17.51x less |
| 'ToonNet (type info)'  | Serialize   | Orders    | 100   |     26.144 us |     0.7038 us |     0.3681 us | 12.50x faster |   0.17x |     5.6458 |         - |         - |    46.26 KB | 18.94x less |
| System.Text.Json       | Serialize   | Orders    | 100   |     26.541 us |     0.3609 us |     0.1602 us | 12.31x faster |   0.09x |     4.9744 |         - |         - |    40.96 KB | 21.39x less |
| Toon.Format            | Serialize   | Orders    | 10000 | 76,767.420 us |   979.4678 us |   512.2806 us |      baseline |         | 10333.3333 | 4000.0000 | 1000.0000 | 87005.79 KB |             |
| ToonNet                | Serialize   | Orders    | 10000 |  2,744.271 us |    10.6304 us |     4.7199 us | 27.97x faster |   0.18x |   167.9688 |  167.9688 |  167.9688 |  4705.64 KB | 18.49x less |
| 'ToonNet (reflection)' | Serialize   | Orders    | 10000 |  4,665.242 us |    28.9487 us |    12.8534 us | 16.46x faster |   0.11x |   203.1250 |  156.2500 |  156.2500 |  5095.34 KB | 17.08x less |
| 'ToonNet (type info)'  | Serialize   | Orders    | 10000 |  2,779.458 us |    45.0944 us |    23.5852 us | 27.62x faster |   0.28x |   167.9688 |  167.9688 |  167.9688 |  4705.79 KB | 18.49x less |
| System.Text.Json       | Serialize   | Orders    | 10000 |  2,829.996 us |    46.5511 us |    24.3471 us | 27.13x faster |   0.28x |   152.3438 |  152.3438 |  152.3438 |  4197.58 KB | 20.73x less |
| Toon.Format            | Serialize   | Products  | 100   |    262.002 us |    18.2397 us |     8.0985 us |      baseline |         |    66.4063 |   21.4844 |         - |   545.25 KB |             |
| ToonNet                | Serialize   | Products  | 100   |     26.373 us |     0.3315 us |     0.1734 us |  9.93x faster |   0.29x |     1.4343 |         - |         - |    11.83 KB | 46.10x less |
| 'ToonNet (reflection)' | Serialize   | Products  | 100   |     31.099 us |     0.6833 us |     0.3034 us |  8.43x faster |   0.26x |     1.3428 |         - |         - |    11.13 KB | 49.01x less |
| 'ToonNet (type info)'  | Serialize   | Products  | 100   |     26.344 us |     0.3264 us |     0.1449 us |  9.95x faster |   0.29x |     1.4343 |         - |         - |    11.95 KB | 45.65x less |
| System.Text.Json       | Serialize   | Products  | 100   |     27.679 us |     0.1754 us |     0.0779 us |  9.47x faster |   0.27x |     3.3264 |         - |         - |    27.45 KB | 19.86x less |
| Toon.Format            | Serialize   | Products  | 10000 | 51,538.510 us | 1,094.5680 us |   485.9950 us |      baseline |         |  6900.0000 | 3000.0000 |  900.0000 | 54374.77 KB |             |
| ToonNet                | Serialize   | Products  | 10000 |  2,740.925 us |    73.4323 us |    38.4065 us | 18.81x faster |   0.30x |   101.5625 |  101.5625 |  101.5625 |   990.27 KB | 54.91x less |
| 'ToonNet (reflection)' | Serialize   | Products  | 10000 |  3,189.240 us |    68.7325 us |    30.5177 us | 16.16x faster |   0.20x |   101.5625 |  101.5625 |  101.5625 |   989.75 KB | 54.94x less |
| 'ToonNet (type info)'  | Serialize   | Products  | 10000 |  2,692.124 us |    34.2992 us |    15.2290 us | 19.14x faster |   0.20x |   101.5625 |  101.5625 |  101.5625 |    990.4 KB | 54.90x less |
| System.Text.Json       | Serialize   | Products  | 10000 |  3,005.230 us |   133.5926 us |    69.8715 us | 17.16x faster |   0.40x |   273.4375 |  273.4375 |  273.4375 |  2767.33 KB | 19.65x less |
| Toon.Format            | Serialize   | Servers   | 100   |    107.473 us |     1.8817 us |     0.8355 us |      baseline |         |    34.4238 |    8.4229 |         - |   281.51 KB |             |
| ToonNet                | Serialize   | Servers   | 100   |      8.730 us |     0.0746 us |     0.0390 us | 12.31x faster |   0.10x |     1.3123 |         - |         - |    10.81 KB | 26.04x less |
| 'ToonNet (reflection)' | Serialize   | Servers   | 100   |      9.669 us |     0.2788 us |     0.1238 us | 11.12x faster |   0.16x |     1.2665 |         - |         - |    10.45 KB | 26.93x less |
| 'ToonNet (type info)'  | Serialize   | Servers   | 100   |      8.578 us |     0.0475 us |     0.0169 us | 12.53x faster |   0.09x |     1.3275 |         - |         - |    10.93 KB | 25.76x less |
| System.Text.Json       | Serialize   | Servers   | 100   |      8.857 us |     0.4390 us |     0.2296 us | 12.14x faster |   0.31x |     1.8768 |         - |         - |    15.37 KB | 18.32x less |
| Toon.Format            | Serialize   | Servers   | 10000 | 23,804.796 us |   506.8567 us |   225.0475 us |      baseline |         |  3812.5000 | 1593.7500 |  562.5000 | 28252.86 KB |             |
| ToonNet                | Serialize   | Servers   | 10000 |    941.245 us |    38.5318 us |    20.1529 us | 25.30x faster |   0.56x |   144.5313 |  144.5313 |  144.5313 |  1025.95 KB | 27.54x less |
| 'ToonNet (reflection)' | Serialize   | Servers   | 10000 |  1,024.579 us |    24.6238 us |    12.8787 us | 23.24x faster |   0.34x |   146.4844 |  146.4844 |  146.4844 |  1025.85 KB | 27.54x less |
| 'ToonNet (type info)'  | Serialize   | Servers   | 10000 |    886.436 us |    10.4876 us |     5.4852 us | 26.86x faster |   0.28x |   138.6719 |  138.6719 |  138.6719 |  1026.13 KB | 27.53x less |
| System.Text.Json       | Serialize   | Servers   | 10000 |    926.520 us |     6.3501 us |     2.8195 us | 25.69x faster |   0.24x |   246.0938 |  246.0938 |  246.0938 |  1573.04 KB | 17.96x less |

In this run "ToonNet" and "ToonNet (type info)" Deserialize Orders 10,000 allocated more than in every other run (6,956 KB
and 6,253 KB instead of 5,550 KB). The cause is not known: it did not show up when the case was measured on its own
(twice: 9,161–9,208 us, 5,550.09 KB), nor under forced gen-2 collections and memory pressure.

## UTF-8 input and output

ToonNet through a string (`Encoding.UTF8.GetString` then `Deserialize`, `Serialize` then `Encoding.UTF8.GetBytes`)
against its UTF-8 overloads (`Deserialize(ReadOnlySpan<byte>)`, `SerializeToUtf8Bytes`, `Serialize` to a reused
`ArrayBufferWriter<byte>`), with System.Text.Json's UTF-8 methods for reference. Ratios are relative to going through a
string.

| Method                           | Categories  | Data      | Count | Mean         | Error         | StdDev      | Ratio        | RatioSD | Gen0     | Gen1     | Gen2     | Allocated  | Alloc Ratio      |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 'ToonNet via string'             | Deserialize | Employees | 100   |    48.712 us |     2.7078 us |   1.4162 us |     baseline |         |   5.3711 |   0.6104 |        - |    45088 B |                  |
| 'ToonNet UTF-8'                  | Deserialize | Employees | 100   |    46.121 us |     0.3113 us |   0.1628 us | 1.06x faster |   0.03x |   3.3569 |   0.2441 |        - |    28288 B |       1.59x less |
| 'System.Text.Json UTF-8'         | Deserialize | Employees | 100   |    32.971 us |     0.3265 us |   0.1450 us | 1.48x faster |   0.04x |   3.0518 |   0.1831 |        - |    25784 B |       1.75x less |
| 'ToonNet via string'             | Deserialize | Employees | 10000 | 5,166.209 us |    19.8384 us |   8.8084 us |     baseline |         | 414.0625 | 265.6250 | 132.8125 |  4188525 B |                  |
| 'ToonNet UTF-8'                  | Deserialize | Employees | 10000 | 4,817.058 us |    27.0420 us |  14.1435 us | 1.07x faster |   0.00x | 281.2500 | 140.6250 |        - |  2404288 B |       1.74x less |
| 'System.Text.Json UTF-8'         | Deserialize | Employees | 10000 | 4,121.062 us |   111.3344 us |  49.4332 us | 1.25x faster |   0.01x | 328.1250 | 203.1250 |  78.1250 |  2583160 B |       1.62x less |
| 'ToonNet via string'             | Deserialize | Orders    | 100   |    83.389 us |     1.9110 us |   0.9995 us |     baseline |         |  12.6953 |   2.4414 |        - |   107064 B |                  |
| 'ToonNet UTF-8'                  | Deserialize | Orders    | 100   |    82.328 us |     2.1610 us |   0.9595 us | 1.01x faster |   0.02x |   7.0801 |   0.8545 |        - |    60216 B |       1.78x less |
| 'System.Text.Json UTF-8'         | Deserialize | Orders    | 100   |    50.441 us |     0.9439 us |   0.4937 us | 1.65x faster |   0.02x |   7.1411 |   0.9766 |        - |    60184 B |       1.78x less |
| 'ToonNet via string'             | Deserialize | Orders    | 10000 | 9,777.261 us |   238.6680 us | 124.8280 us |     baseline |         | 765.6250 | 421.8750 |  93.7500 | 10500203 B |                  |
| 'ToonNet UTF-8'                  | Deserialize | Orders    | 10000 | 9,607.028 us |   263.0707 us | 137.5911 us | 1.02x faster |   0.02x | 750.0000 | 375.0000 |        - |  6403344 B |       1.64x less |
| 'System.Text.Json UTF-8'         | Deserialize | Orders    | 10000 | 8,963.572 us | 1,062.6663 us | 555.7950 us | 1.09x faster |   0.06x | 812.5000 | 500.0000 | 187.5000 |  6023279 B |       1.74x less |
| 'ToonNet via string'             | Deserialize | Products  | 100   |    42.757 us |     1.4388 us |   0.7525 us |     baseline |         |   5.8594 |   0.6104 |        - |    49112 B |                  |
| 'ToonNet UTF-8'                  | Deserialize | Products  | 100   |    43.344 us |     1.0220 us |   0.5345 us | 1.01x slower |   0.02x |   4.6997 |   0.4272 |        - |    39368 B |       1.25x less |
| 'System.Text.Json UTF-8'         | Deserialize | Products  | 100   |    42.747 us |     0.9688 us |   0.4302 us | 1.00x slower |   0.02x |   4.3945 |   0.3662 |        - |    36984 B |       1.33x less |
| 'ToonNet via string'             | Deserialize | Products  | 10000 | 5,011.672 us |    95.3140 us |  49.8511 us |     baseline |         | 515.6250 | 304.6875 | 101.5625 |  4535159 B |                  |
| 'ToonNet UTF-8'                  | Deserialize | Products  | 10000 | 5,257.427 us |    81.7970 us |  36.3184 us | 1.05x slower |   0.01x | 414.0625 | 203.1250 |        - |  3524168 B |       1.29x less |
| 'System.Text.Json UTF-8'         | Deserialize | Products  | 10000 | 6,251.393 us |   139.7313 us |  73.0822 us | 1.25x slower |   0.02x | 484.3750 | 218.7500 |  78.1250 |  3703033 B |       1.22x less |
| 'ToonNet via string'             | Deserialize | Servers   | 100   |    20.825 us |     0.6124 us |   0.3203 us |     baseline |         |   5.3711 |   0.4272 |        - |    45112 B |                  |
| 'ToonNet UTF-8'                  | Deserialize | Servers   | 100   |    21.068 us |     1.0819 us |   0.5659 us | 1.01x slower |   0.03x |   4.3945 |   0.3967 |        - |    36824 B |       1.23x less |
| 'System.Text.Json UTF-8'         | Deserialize | Servers   | 100   |    17.712 us |     0.5759 us |   0.3012 us | 1.18x faster |   0.03x |   3.1738 |   0.2441 |        - |    26664 B |       1.69x less |
| 'ToonNet via string'             | Deserialize | Servers   | 10000 | 3,611.460 us |   117.6648 us |  61.5410 us |     baseline |         | 484.3750 | 343.7500 | 242.1875 |  4176449 B |                  |
| 'ToonNet UTF-8'                  | Deserialize | Servers   | 10000 | 3,493.787 us |    31.5739 us |  14.0190 us | 1.03x faster |   0.02x | 406.2500 | 257.8125 | 160.1563 |  3288178 B |       1.27x less |
| 'System.Text.Json UTF-8'         | Deserialize | Servers   | 10000 | 2,724.200 us |    19.2083 us |  10.0463 us | 1.33x faster |   0.02x | 335.9375 | 214.8438 | 128.9063 |  2612645 B |       1.60x less |
| 'ToonNet via string'             | Serialize   | Employees | 100   |    18.799 us |     0.0657 us |   0.0292 us |     baseline |         |   3.0518 |   0.1526 |        - |    25752 B |                  |
| 'ToonNet SerializeToUtf8Bytes'   | Serialize   | Employees | 100   |    19.744 us |     0.6460 us |   0.3378 us | 1.05x slower |   0.02x |   1.0681 |   0.0305 |        - |     9000 B |       2.86x less |
| 'ToonNet IBufferWriter (reused)' | Serialize   | Employees | 100   |    17.855 us |     0.0770 us |   0.0275 us | 1.05x faster |   0.00x |   0.0610 |        - |        - |      584 B |      44.10x less |
| 'System.Text.Json UTF-8'         | Serialize   | Employees | 100   |    20.267 us |     1.2334 us |   0.6451 us | 1.08x slower |   0.03x |   2.0447 |        - |        - |    17160 B |       1.50x less |
| 'ToonNet via string'             | Serialize   | Employees | 10000 | 2,148.224 us |    78.4236 us |  41.0171 us |     baseline |         | 144.5313 | 144.5313 | 144.5313 |  2677568 B |                  |
| 'ToonNet SerializeToUtf8Bytes'   | Serialize   | Employees | 10000 | 1,964.194 us |     9.0683 us |   4.0264 us | 1.09x faster |   0.02x |  85.9375 |  85.9375 |  85.9375 |   893584 B |      2.996x less |
| 'ToonNet IBufferWriter (reused)' | Serialize   | Employees | 10000 | 1,868.893 us |    32.8953 us |  14.6057 us | 1.15x faster |   0.02x |        - |        - |        - |      856 B |  3,128.000x less |
| 'System.Text.Json UTF-8'         | Serialize   | Employees | 10000 | 2,145.497 us |    88.7714 us |  46.4292 us | 1.00x faster |   0.03x | 125.0000 | 125.0000 | 125.0000 |  1743320 B |      1.536x less |
| 'ToonNet via string'             | Serialize   | Orders    | 100   |    27.602 us |     0.2264 us |   0.1005 us |     baseline |         |   8.4229 |   0.7019 |        - |    70808 B |                  |
| 'ToonNet SerializeToUtf8Bytes'   | Serialize   | Orders    | 100   |    26.326 us |     0.1863 us |   0.0974 us | 1.05x faster |   0.01x |   2.8381 |   0.2136 |        - |    24008 B |      2.949x less |
| 'ToonNet IBufferWriter (reused)' | Serialize   | Orders    | 100   |    25.371 us |     0.2612 us |   0.1366 us | 1.09x faster |   0.01x |   0.0610 |        - |        - |      568 B |    124.662x less |
| 'System.Text.Json UTF-8'         | Serialize   | Orders    | 100   |    25.923 us |     1.0091 us |   0.5278 us | 1.07x faster |   0.02x |   2.5024 |        - |        - |    21144 B |      3.349x less |
| 'ToonNet via string'             | Serialize   | Orders    | 10000 | 3,211.998 us |    68.7346 us |  35.9495 us |     baseline |         | 156.2500 | 156.2500 | 156.2500 |  7226270 B |                  |
| 'ToonNet SerializeToUtf8Bytes'   | Serialize   | Orders    | 10000 | 2,810.273 us |   101.8048 us |  53.2459 us | 1.14x faster |   0.02x | 113.2813 | 113.2813 | 113.2813 |  2409882 B |      2.999x less |
| 'ToonNet IBufferWriter (reused)' | Serialize   | Orders    | 10000 | 2,538.257 us |    23.1494 us |  10.2785 us | 1.27x faster |   0.01x |        - |        - |        - |      720 B | 10,036.486x less |
| 'System.Text.Json UTF-8'         | Serialize   | Orders    | 10000 | 3,056.418 us |    33.8765 us |  15.0414 us | 1.05x faster |   0.01x |  97.6563 |  97.6563 |  97.6563 |  2149389 B |      3.362x less |
| 'ToonNet via string'             | Serialize   | Products  | 100   |    26.846 us |     0.4504 us |   0.2356 us |     baseline |         |   2.0447 |   0.0610 |        - |    17120 B |                  |
| 'ToonNet SerializeToUtf8Bytes'   | Serialize   | Products  | 100   |    26.429 us |     0.1621 us |   0.0848 us | 1.02x faster |   0.01x |   0.8850 |        - |        - |     7424 B |       2.31x less |
| 'ToonNet IBufferWriter (reused)' | Serialize   | Products  | 100   |    26.246 us |     0.1221 us |   0.0639 us | 1.02x faster |   0.01x |   0.2747 |        - |        - |     2536 B |       6.75x less |
| 'System.Text.Json UTF-8'         | Serialize   | Products  | 100   |    26.914 us |     0.6437 us |   0.3367 us | 1.00x slower |   0.01x |   1.6785 |        - |        - |    14232 B |       1.20x less |
| 'ToonNet via string'             | Serialize   | Products  | 10000 | 2,808.474 us |    78.8509 us |  35.0103 us |     baseline |         | 175.7813 | 175.7813 | 175.7813 |  1520069 B |                  |
| 'ToonNet SerializeToUtf8Bytes'   | Serialize   | Products  | 10000 | 2,679.568 us |    38.1964 us |  16.9594 us | 1.05x faster |   0.01x |  58.5938 |  58.5938 |  58.5938 |   508468 B |      2.990x less |
| 'ToonNet IBufferWriter (reused)' | Serialize   | Products  | 10000 | 2,610.277 us |    12.7896 us |   5.6787 us | 1.08x faster |   0.01x |        - |        - |        - |     2656 B |    572.315x less |
| 'System.Text.Json UTF-8'         | Serialize   | Products  | 10000 | 2,838.924 us |    49.4935 us |  21.9754 us | 1.01x slower |   0.01x | 187.5000 | 187.5000 | 187.5000 |  1417454 B |      1.072x less |
| 'ToonNet via string'             | Serialize   | Servers   | 100   |     9.073 us |     0.0199 us |   0.0088 us |     baseline |         |   1.8311 |   0.0458 |        - |    15352 B |                  |
| 'ToonNet SerializeToUtf8Bytes'   | Serialize   | Servers   | 100   |     8.704 us |     0.0382 us |   0.0200 us | 1.04x faster |   0.00x |   0.8392 |        - |        - |     7112 B |       2.16x less |
| 'ToonNet IBufferWriter (reused)' | Serialize   | Servers   | 100   |     8.632 us |     0.2548 us |   0.1332 us | 1.05x faster |   0.02x |   0.3510 |        - |        - |     2952 B |       5.20x less |
| 'System.Text.Json UTF-8'         | Serialize   | Servers   | 100   |     8.402 us |     0.1132 us |   0.0502 us | 1.08x faster |   0.01x |   0.9460 |        - |        - |     8040 B |       1.91x less |
| 'ToonNet via string'             | Serialize   | Servers   | 10000 |   980.490 us |     3.3881 us |   1.5043 us |     baseline |         | 251.9531 | 251.9531 | 251.9531 |  1495644 B |                  |
| 'ToonNet SerializeToUtf8Bytes'   | Serialize   | Servers   | 10000 |   913.220 us |     9.1646 us |   4.0691 us | 1.07x faster |   0.00x | 113.2813 | 113.2813 | 113.2813 |   606400 B |       2.47x less |
| 'ToonNet IBufferWriter (reused)' | Serialize   | Servers   | 10000 |   874.477 us |    14.2747 us |   7.4660 us | 1.12x faster |   0.01x |  33.2031 |  33.2031 |  33.2031 |   161761 B |       9.25x less |
| 'System.Text.Json UTF-8'         | Serialize   | Servers   | 10000 |   921.328 us |    20.1524 us |  10.5401 us | 1.06x faster |   0.01x | 164.0625 | 164.0625 | 164.0625 |   805851 B |       1.86x less |
