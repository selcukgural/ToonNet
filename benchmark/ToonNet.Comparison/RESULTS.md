# ToonNet vs Toon.Format – benchmark results

Measured with `dotnet run -c Release -- --filter "*" --launchCount 1 --warmupCount 3 --iterationCount 8` in this
directory on 2026-10-14 (one run for both tables).

```
BenchmarkDotNet v0.15.8, macOS Tahoe 26.6.2 (25G83) [Darwin 25.6.0]
Apple M3 Max, 1 CPU, 16 logical and 16 physical cores
.NET SDK 10.0.105, .NET 10.0.5 (Arm64 RyuJIT armv8.0-a)
Toon.Format 1.0.0, ToonNet 3.2.0
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

| Method                 | Categories  | Data      | Count | Mean          | Error         | StdDev      | Ratio         | RatioSD | Gen0       | Gen1      | Gen2      | Allocated   | Alloc Ratio |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Toon.Format            | Deserialize | Employees | 100   |    140.263 us |     3.2373 us |   1.6932 us |      baseline |         |    42.9688 |   11.7188 |         - |   350.97 KB |             |
| ToonNet                | Deserialize | Employees | 100   |     48.308 us |     0.9316 us |   0.4872 us |  2.90x faster |   0.04x |     3.3569 |    0.0610 |         - |    27.51 KB | 12.76x less |
| 'ToonNet (reflection)' | Deserialize | Employees | 100   |     55.373 us |     0.1609 us |   0.0574 us |  2.53x faster |   0.03x |     3.3569 |    0.2441 |         - |    27.48 KB | 12.77x less |
| 'ToonNet (type info)'  | Deserialize | Employees | 100   |     46.315 us |     0.8214 us |   0.3647 us |  3.03x faster |   0.04x |     3.3569 |    0.2441 |         - |    27.63 KB | 12.70x less |
| System.Text.Json       | Deserialize | Employees | 100   |     33.450 us |     0.7522 us |   0.3934 us |  4.19x faster |   0.07x |     3.0518 |    0.1831 |         - |    25.18 KB | 13.94x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Toon.Format            | Deserialize | Employees | 10000 | 31,487.516 us |   288.9788 us | 128.3084 us |      baseline |         |  5062.5000 | 2343.7500 |  875.0000 | 35153.25 KB |             |
| ToonNet                | Deserialize | Employees | 10000 |  4,791.835 us |    10.3370 us |   4.5897 us |  6.57x faster |   0.03x |   281.2500 |  140.6250 |         - |  2347.82 KB | 14.97x less |
| 'ToonNet (reflection)' | Deserialize | Employees | 10000 |  5,643.848 us |    23.2246 us |  10.3119 us |  5.58x faster |   0.02x |   281.2500 |  140.6250 |         - |  2347.79 KB | 14.97x less |
| 'ToonNet (type info)'  | Deserialize | Employees | 10000 |  4,888.753 us |    22.8009 us |  10.1237 us |  6.44x faster |   0.03x |   281.2500 |  140.6250 |         - |  2347.95 KB | 14.97x less |
| System.Text.Json       | Deserialize | Employees | 10000 |  4,222.731 us |    69.5662 us |  30.8878 us |  7.46x faster |   0.06x |   328.1250 |  203.1250 |   78.1250 |  2522.66 KB | 13.93x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Toon.Format            | Deserialize | Orders    | 100   |    245.774 us |     3.6700 us |   1.6295 us |      baseline |         |    99.1211 |   38.5742 |         - |   809.82 KB |             |
| ToonNet                | Deserialize | Orders    | 100   |     80.910 us |     0.3892 us |   0.1728 us |  3.04x faster |   0.02x |     7.9346 |    1.0986 |         - |    64.88 KB | 12.48x less |
| 'ToonNet (reflection)' | Deserialize | Orders    | 100   |     96.869 us |     0.2550 us |   0.1132 us |  2.54x faster |   0.02x |     7.0801 |    0.9766 |         - |    58.66 KB | 13.81x less |
| 'ToonNet (type info)'  | Deserialize | Orders    | 100   |     79.262 us |     1.1405 us |   0.5064 us |  3.10x faster |   0.03x |     7.0801 |    0.8545 |         - |    58.81 KB | 13.77x less |
| System.Text.Json       | Deserialize | Orders    | 100   |     50.784 us |     0.2986 us |   0.1326 us |  4.84x faster |   0.03x |     7.1411 |    0.9766 |         - |    58.77 KB | 13.78x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Toon.Format            | Deserialize | Orders    | 10000 | 68,392.723 us | 1,568.2330 us | 820.2162 us |      baseline |         | 10500.0000 | 4750.0000 | 1500.0000 | 80186.29 KB |             |
| ToonNet                | Deserialize | Orders    | 10000 |  9,143.860 us |   217.3800 us | 113.6939 us |  7.48x faster |   0.12x |   671.8750 |  328.1250 |         - |  5550.09 KB | 14.45x less |
| 'ToonNet (reflection)' | Deserialize | Orders    | 10000 | 10,797.979 us |   205.6762 us | 107.5726 us |  6.33x faster |   0.09x |   671.8750 |  328.1250 |         - |  5550.06 KB | 14.45x less |
| 'ToonNet (type info)'  | Deserialize | Orders    | 10000 |  9,463.882 us |   223.7140 us | 117.0068 us |  7.23x faster |   0.12x |   671.8750 |  328.1250 |         - |  5550.22 KB | 14.45x less |
| System.Text.Json       | Deserialize | Orders    | 10000 |  8,492.905 us |    59.3171 us |  21.1531 us |  8.05x faster |   0.09x |   812.5000 |  500.0000 |  187.5000 |   5882.3 KB | 13.63x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Toon.Format            | Deserialize | Products  | 100   |    140.473 us |     0.6676 us |   0.2964 us |      baseline |         |    36.1328 |   10.2539 |         - |   295.87 KB |             |
| ToonNet                | Deserialize | Products  | 100   |     44.320 us |     0.5008 us |   0.2224 us |  3.17x faster |   0.02x |     4.6387 |    0.4272 |         - |    38.33 KB |  7.72x less |
| 'ToonNet (reflection)' | Deserialize | Products  | 100   |     56.521 us |     1.4918 us |   0.7803 us |  2.49x faster |   0.03x |     4.6387 |    0.4272 |         - |     38.3 KB |  7.73x less |
| 'ToonNet (type info)'  | Deserialize | Products  | 100   |     42.353 us |     0.8703 us |   0.4552 us |  3.32x faster |   0.03x |     4.6997 |    0.3662 |         - |    38.45 KB |  7.69x less |
| System.Text.Json       | Deserialize | Products  | 100   |     42.666 us |     0.9747 us |   0.5098 us |  3.29x faster |   0.04x |     4.3945 |    0.3662 |         - |    36.12 KB |  8.19x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Toon.Format            | Deserialize | Products  | 10000 | 30,917.353 us |   214.3679 us |  95.1807 us |      baseline |         |  4281.2500 | 2000.0000 |  843.7500 | 29472.67 KB |             |
| ToonNet                | Deserialize | Products  | 10000 |  4,851.616 us |    15.7080 us |   6.9745 us |  6.37x faster |   0.02x |   414.0625 |  203.1250 |         - |  3441.45 KB |  8.56x less |
| 'ToonNet (reflection)' | Deserialize | Products  | 10000 |  5,496.111 us |    58.3988 us |  30.5437 us |  5.63x faster |   0.03x |   414.0625 |  203.1250 |         - |  3441.42 KB |  8.56x less |
| 'ToonNet (type info)'  | Deserialize | Products  | 10000 |  4,466.864 us |    19.5363 us |   8.6742 us |  6.92x faster |   0.02x |   414.0625 |  203.1250 |         - |  3441.58 KB |  8.56x less |
| System.Text.Json       | Deserialize | Products  | 10000 |  6,308.815 us |   152.7345 us |  67.8151 us |  4.90x faster |   0.05x |   484.3750 |  218.7500 |   78.1250 |   3616.3 KB |  8.15x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Toon.Format            | Deserialize | Servers   | 100   |     65.813 us |     0.5901 us |   0.2620 us |      baseline |         |    26.8555 |    5.8594 |         - |    223.3 KB |             |
| ToonNet                | Deserialize | Servers   | 100   |     20.669 us |     0.2946 us |   0.1541 us |  3.18x faster |   0.03x |     4.3640 |    0.4272 |         - |    35.84 KB |  6.23x less |
| 'ToonNet (reflection)' | Deserialize | Servers   | 100   |     24.786 us |     0.0726 us |   0.0322 us |  2.66x faster |   0.01x |     4.3640 |    0.3357 |         - |    35.81 KB |  6.24x less |
| 'ToonNet (type info)'  | Deserialize | Servers   | 100   |     18.947 us |     0.2335 us |   0.1037 us |  3.47x faster |   0.02x |     4.3945 |    0.3662 |         - |    35.97 KB |  6.21x less |
| System.Text.Json       | Deserialize | Servers   | 100   |     17.602 us |     0.1672 us |   0.0875 us |  3.74x faster |   0.02x |     3.1738 |    0.2441 |         - |    26.04 KB |  8.58x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Toon.Format            | Deserialize | Servers   | 10000 | 15,534.025 us |   562.3125 us | 294.1003 us |      baseline |         |  2843.7500 | 1375.0000 |  531.2500 | 22392.62 KB |             |
| ToonNet                | Deserialize | Servers   | 10000 |  3,495.643 us |    41.1661 us |  18.2780 us |  4.44x faster |   0.08x |   402.3438 |  257.8125 |  156.2500 |  3211.06 KB |  6.97x less |
| 'ToonNet (reflection)' | Deserialize | Servers   | 10000 |  3,698.796 us |    74.0321 us |  38.7202 us |  4.20x faster |   0.09x |   398.4375 |  253.9063 |  156.2500 |  3211.04 KB |  6.97x less |
| 'ToonNet (type info)'  | Deserialize | Servers   | 10000 |  3,252.232 us |    54.3416 us |  19.3787 us |  4.78x faster |   0.09x |   402.3438 |  253.9063 |  160.1563 |  3211.21 KB |  6.97x less |
| System.Text.Json       | Deserialize | Servers   | 10000 |  2,811.697 us |    78.8209 us |  41.2248 us |  5.53x faster |   0.12x |   285.1563 |  152.3438 |   70.3125 |  2551.22 KB |  8.78x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Toon.Format            | Serialize   | Employees | 100   |    226.235 us |     1.6885 us |   0.6021 us |      baseline |         |    62.5000 |   27.3438 |         - |   525.56 KB |             |
| ToonNet                | Serialize   | Employees | 100   |     19.507 us |     0.2773 us |   0.1231 us | 11.60x faster |   0.07x |     2.0447 |         - |         - |    16.81 KB | 31.26x less |
| 'ToonNet (reflection)' | Serialize   | Employees | 100   |     24.723 us |     0.3033 us |   0.1346 us |  9.15x faster |   0.05x |     2.0447 |         - |         - |    16.81 KB | 31.26x less |
| 'ToonNet (type info)'  | Serialize   | Employees | 100   |     18.182 us |     0.2986 us |   0.1326 us | 12.44x faster |   0.09x |     2.0447 |         - |         - |    16.94 KB | 31.03x less |
| System.Text.Json       | Serialize   | Employees | 100   |     20.844 us |     0.4596 us |   0.2041 us | 10.85x faster |   0.10x |     4.0283 |         - |         - |    33.19 KB | 15.84x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Toon.Format            | Serialize   | Employees | 10000 | 45,732.628 us |   733.9439 us | 325.8756 us |      baseline |         |  6818.1818 | 3181.8182 | 1000.0000 | 53017.27 KB |             |
| ToonNet                | Serialize   | Employees | 10000 |  1,967.613 us |     7.3481 us |   2.6204 us | 23.24x faster |   0.16x |   152.3438 |  152.3438 |  152.3438 |  1744.23 KB | 30.40x less |
| 'ToonNet (reflection)' | Serialize   | Employees | 10000 |  2,626.979 us |    14.0396 us |   6.2337 us | 17.41x faster |   0.12x |   152.3438 |  152.3438 |  152.3438 |  1744.29 KB | 30.39x less |
| 'ToonNet (type info)'  | Serialize   | Employees | 10000 |  1,993.216 us |     6.2725 us |   3.2807 us | 22.94x faster |   0.16x |   152.3438 |  152.3438 |  152.3438 |  1744.34 KB | 30.39x less |
| System.Text.Json       | Serialize   | Employees | 10000 |  2,228.001 us |    10.8012 us |   3.8518 us | 20.53x faster |   0.14x |   234.3750 |  234.3750 |  234.3750 |   3404.7 KB | 15.57x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Toon.Format            | Serialize   | Orders    | 100   |    319.806 us |     9.0322 us |   4.7240 us |      baseline |         |   105.4688 |   35.1563 |         - |   876.35 KB |             |
| ToonNet                | Serialize   | Orders    | 100   |     25.606 us |     0.0781 us |   0.0347 us | 12.49x faster |   0.17x |     5.6152 |         - |         - |    46.14 KB | 18.99x less |
| 'ToonNet (reflection)' | Serialize   | Orders    | 100   |     38.669 us |     0.1093 us |   0.0485 us |  8.27x faster |   0.12x |     5.6152 |         - |         - |    46.14 KB | 18.99x less |
| 'ToonNet (type info)'  | Serialize   | Orders    | 100   |     25.744 us |     0.1936 us |   0.1013 us | 12.42x faster |   0.18x |     5.6458 |         - |         - |    46.27 KB | 18.94x less |
| System.Text.Json       | Serialize   | Orders    | 100   |     26.531 us |     0.3092 us |   0.1373 us | 12.05x faster |   0.18x |     4.9744 |         - |         - |    40.96 KB | 21.39x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Toon.Format            | Serialize   | Orders    | 10000 | 77,849.303 us |   918.3881 us | 407.7700 us |      baseline |         | 10714.2857 | 4428.5714 | 1285.7143 | 87006.52 KB |             |
| ToonNet                | Serialize   | Orders    | 10000 |  2,757.223 us |     8.1914 us |   2.9211 us | 28.23x faster |   0.14x |   156.2500 |  156.2500 |  156.2500 |  4705.62 KB | 18.49x less |
| 'ToonNet (reflection)' | Serialize   | Orders    | 10000 |  4,068.457 us |    13.7080 us |   6.0864 us | 19.13x faster |   0.10x |   140.6250 |  140.6250 |  140.6250 |  4705.42 KB | 18.49x less |
| 'ToonNet (type info)'  | Serialize   | Orders    | 10000 |  2,751.023 us |     9.3151 us |   4.1359 us | 28.30x faster |   0.14x |   156.2500 |  156.2500 |  156.2500 |  4705.76 KB | 18.49x less |
| System.Text.Json       | Serialize   | Orders    | 10000 |  2,939.129 us |    11.3084 us |   4.0327 us | 26.49x faster |   0.13x |   140.6250 |  140.6250 |  140.6250 |  4197.46 KB | 20.73x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Toon.Format            | Serialize   | Products  | 100   |    254.399 us |     3.0876 us |   1.6149 us |      baseline |         |    66.4063 |   21.4844 |         - |   545.25 KB |             |
| ToonNet                | Serialize   | Products  | 100   |     26.342 us |     0.1773 us |   0.0787 us |  9.66x faster |   0.06x |     1.4343 |         - |         - |    11.83 KB | 46.10x less |
| 'ToonNet (reflection)' | Serialize   | Products  | 100   |     30.089 us |     0.1675 us |   0.0876 us |  8.45x faster |   0.06x |     1.3428 |         - |         - |    11.13 KB | 49.01x less |
| 'ToonNet (type info)'  | Serialize   | Products  | 100   |     25.732 us |     0.2008 us |   0.0892 us |  9.89x faster |   0.07x |     1.4343 |         - |         - |    11.95 KB | 45.62x less |
| System.Text.Json       | Serialize   | Products  | 100   |     27.497 us |     0.5165 us |   0.2701 us |  9.25x faster |   0.10x |     3.3264 |         - |         - |    27.45 KB | 19.86x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Toon.Format            | Serialize   | Products  | 10000 | 48,973.735 us |   582.3739 us | 258.5776 us |      baseline |         |  6750.0000 | 3000.0000 |  750.0000 | 54374.39 KB |             |
| ToonNet                | Serialize   | Products  | 10000 |  2,683.609 us |     3.7963 us |   1.6856 us | 18.25x faster |   0.09x |   101.5625 |  101.5625 |  101.5625 |   990.27 KB | 54.91x less |
| 'ToonNet (reflection)' | Serialize   | Products  | 10000 |  3,184.822 us |    48.7950 us |  25.5208 us | 15.38x faster |   0.14x |   101.5625 |  101.5625 |  101.5625 |   989.78 KB | 54.94x less |
| 'ToonNet (type info)'  | Serialize   | Products  | 10000 |  2,693.770 us |    43.8717 us |  22.9457 us | 18.18x faster |   0.17x |   101.5625 |  101.5625 |  101.5625 |   990.41 KB | 54.90x less |
| System.Text.Json       | Serialize   | Products  | 10000 |  2,949.974 us |    16.9897 us |   7.5435 us | 16.60x faster |   0.09x |   273.4375 |  273.4375 |  273.4375 |  2767.36 KB | 19.65x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Toon.Format            | Serialize   | Servers   | 100   |    107.105 us |     1.3516 us |   0.4820 us |      baseline |         |    34.1797 |    7.8125 |         - |   281.51 KB |             |
| ToonNet                | Serialize   | Servers   | 100   |      8.753 us |     0.0147 us |   0.0065 us | 12.24x faster |   0.05x |     1.3123 |         - |         - |    10.81 KB | 26.04x less |
| 'ToonNet (reflection)' | Serialize   | Servers   | 100   |      9.463 us |     0.0342 us |   0.0152 us | 11.32x faster |   0.05x |     1.2665 |         - |         - |    10.45 KB | 26.93x less |
| 'ToonNet (type info)'  | Serialize   | Servers   | 100   |      8.656 us |     0.0336 us |   0.0149 us | 12.37x faster |   0.06x |     1.3275 |         - |         - |    10.94 KB | 25.74x less |
| System.Text.Json       | Serialize   | Servers   | 100   |      8.868 us |     0.2558 us |   0.1338 us | 12.08x faster |   0.18x |     1.8768 |         - |         - |    15.37 KB | 18.32x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Toon.Format            | Serialize   | Servers   | 10000 | 23,690.709 us |   120.3503 us |  53.4363 us |      baseline |         |  3812.5000 | 1593.7500 |  562.5000 | 28252.47 KB |             |
| ToonNet                | Serialize   | Servers   | 10000 |    890.969 us |    10.8185 us |   4.8035 us | 26.59x faster |   0.15x |   146.4844 |  146.4844 |  146.4844 |  1025.93 KB | 27.54x less |
| 'ToonNet (reflection)' | Serialize   | Servers   | 10000 |    975.134 us |    14.2666 us |   7.4617 us | 24.30x faster |   0.18x |   147.4609 |  147.4609 |  147.4609 |  1025.86 KB | 27.54x less |
| 'ToonNet (type info)'  | Serialize   | Servers   | 10000 |    880.286 us |     2.6008 us |   0.9275 us | 26.91x faster |   0.06x |   145.5078 |  145.5078 |  145.5078 |   1026.1 KB | 27.53x less |
| System.Text.Json       | Serialize   | Servers   | 10000 |    924.452 us |    14.4270 us |   7.5456 us | 25.63x faster |   0.20x |   247.0703 |  247.0703 |  247.0703 |  1573.05 KB | 17.96x less |

In this run "ToonNet" Deserialize Orders 100 allocated 64.88 KB; counted exactly with `gate --report` in this project, it
allocates 60,224 B per operation, like "ToonNet (type info)", which runs the same code.

## UTF-8 input and output

ToonNet through a string (`Encoding.UTF8.GetString` then `Deserialize`, `Serialize` then `Encoding.UTF8.GetBytes`)
against its UTF-8 overloads (`Deserialize(ReadOnlySpan<byte>)`, `SerializeToUtf8Bytes`, `Serialize` to a reused
`ArrayBufferWriter<byte>`), with System.Text.Json's UTF-8 methods for reference. Ratios are relative to going through a
string.

| Method                           | Categories  | Data      | Count | Mean         | Error       | StdDev     | Ratio        | RatioSD | Gen0     | Gen1     | Gen2     | Allocated  | Alloc Ratio     |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 'ToonNet via string'             | Deserialize | Employees | 100   |    47.308 us |   0.5233 us |  0.2324 us |     baseline |         |   5.3711 |   0.5493 |        - |    45096 B |                 |
| 'ToonNet UTF-8'                  | Deserialize | Employees | 100   |    47.313 us |   0.9072 us |  0.4028 us | 1.00x slower |   0.01x |   3.3569 |   0.2441 |        - |    28296 B |      1.59x less |
| 'System.Text.Json UTF-8'         | Deserialize | Employees | 100   |    34.102 us |   0.4815 us |  0.2138 us | 1.39x faster |   0.01x |   3.0518 |   0.1831 |        - |    25784 B |      1.75x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 'ToonNet via string'             | Deserialize | Employees | 10000 | 5,133.840 us | 117.0014 us | 51.9493 us |     baseline |         | 414.0625 | 265.6250 | 132.8125 |  4188532 B |                 |
| 'ToonNet UTF-8'                  | Deserialize | Employees | 10000 | 4,875.352 us | 135.3742 us | 70.8033 us | 1.05x faster |   0.02x | 281.2500 | 140.6250 |        - |  2404296 B |      1.74x less |
| 'System.Text.Json UTF-8'         | Deserialize | Employees | 10000 | 4,125.685 us |  60.6181 us | 31.7045 us | 1.24x faster |   0.01x | 328.1250 | 203.1250 |  78.1250 |  2583112 B |      1.62x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 'ToonNet via string'             | Deserialize | Orders    | 100   |    84.489 us |   1.0880 us |  0.5690 us |     baseline |         |  12.6953 |   2.5635 |        - |   107072 B |                 |
| 'ToonNet UTF-8'                  | Deserialize | Orders    | 100   |    82.217 us |   0.8875 us |  0.4642 us | 1.03x faster |   0.01x |   7.0801 |   0.8545 |        - |    60224 B |      1.78x less |
| 'System.Text.Json UTF-8'         | Deserialize | Orders    | 100   |    50.312 us |   0.8416 us |  0.4401 us | 1.68x faster |   0.02x |   7.1411 |   0.9766 |        - |    60184 B |      1.78x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 'ToonNet via string'             | Deserialize | Orders    | 10000 | 9,813.909 us |  87.6214 us | 45.8277 us |     baseline |         | 781.2500 | 437.5000 | 109.3750 | 10500484 B |                 |
| 'ToonNet UTF-8'                  | Deserialize | Orders    | 10000 | 9,239.330 us |  75.1992 us | 33.3889 us | 1.06x faster |   0.01x | 671.8750 | 328.1250 |        - |  5683424 B |      1.85x less |
| 'System.Text.Json UTF-8'         | Deserialize | Orders    | 10000 | 8,342.123 us |  68.7581 us | 30.5290 us | 1.18x faster |   0.01x | 812.5000 | 500.0000 | 187.5000 |  6023311 B |      1.74x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 'ToonNet via string'             | Deserialize | Products  | 100   |    45.427 us |   0.3848 us |  0.1709 us |     baseline |         |   5.8594 |   0.6104 |        - |    49120 B |                 |
| 'ToonNet UTF-8'                  | Deserialize | Products  | 100   |    41.482 us |   0.7907 us |  0.3511 us | 1.10x faster |   0.01x |   4.6997 |   0.3662 |        - |    39376 B |      1.25x less |
| 'System.Text.Json UTF-8'         | Deserialize | Products  | 100   |    42.088 us |   0.6850 us |  0.3041 us | 1.08x faster |   0.01x |   4.3945 |   0.3662 |        - |    36984 B |      1.33x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 'ToonNet via string'             | Deserialize | Products  | 10000 | 4,983.259 us | 110.4028 us | 49.0195 us |     baseline |         | 523.4375 | 312.5000 | 101.5625 |  4535167 B |                 |
| 'ToonNet UTF-8'                  | Deserialize | Products  | 10000 | 4,683.684 us |  11.2586 us |  4.0149 us | 1.06x faster |   0.01x | 414.0625 | 203.1250 |        - |  3524176 B |      1.29x less |
| 'System.Text.Json UTF-8'         | Deserialize | Products  | 10000 | 6,156.082 us |  80.9292 us | 35.9331 us | 1.24x slower |   0.01x | 484.3750 | 218.7500 |  78.1250 |  3703017 B |      1.22x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 'ToonNet via string'             | Deserialize | Servers   | 100   |    21.100 us |   0.6416 us |  0.3356 us |     baseline |         |   5.3711 |   0.4883 |        - |    45120 B |                 |
| 'ToonNet UTF-8'                  | Deserialize | Servers   | 100   |    21.391 us |   0.0288 us |  0.0103 us | 1.01x slower |   0.02x |   4.3945 |   0.3662 |        - |    36832 B |      1.23x less |
| 'System.Text.Json UTF-8'         | Deserialize | Servers   | 100   |    17.444 us |   0.5405 us |  0.2400 us | 1.21x faster |   0.02x |   3.1738 |   0.2441 |        - |    26664 B |      1.69x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 'ToonNet via string'             | Deserialize | Servers   | 10000 | 3,595.414 us |  72.2624 us | 37.7946 us |     baseline |         | 492.1875 | 355.4688 | 246.0938 |  4176500 B |                 |
| 'ToonNet UTF-8'                  | Deserialize | Servers   | 10000 | 3,571.312 us |  41.1280 us | 18.2611 us | 1.01x faster |   0.01x | 406.2500 | 253.9063 | 160.1563 |  3288214 B |      1.27x less |
| 'System.Text.Json UTF-8'         | Deserialize | Servers   | 10000 | 2,767.036 us |   6.9814 us |  3.0998 us | 1.30x faster |   0.01x | 335.9375 | 214.8438 | 128.9063 |  2612728 B |      1.60x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 'ToonNet via string'             | Serialize   | Employees | 100   |    18.975 us |   0.1312 us |  0.0582 us |     baseline |         |   3.0518 |   0.1526 |        - |    25760 B |                 |
| 'ToonNet SerializeToUtf8Bytes'   | Serialize   | Employees | 100   |    18.397 us |   0.3608 us |  0.1602 us | 1.03x faster |   0.01x |   1.0681 |   0.0305 |        - |     9008 B |      2.86x less |
| 'ToonNet IBufferWriter (reused)' | Serialize   | Employees | 100   |    18.017 us |   0.3350 us |  0.1752 us | 1.05x faster |   0.01x |   0.0610 |        - |        - |      592 B |     43.51x less |
| 'System.Text.Json UTF-8'         | Serialize   | Employees | 100   |    19.927 us |   0.5550 us |  0.2903 us | 1.05x slower |   0.01x |   2.0447 |        - |        - |    17160 B |      1.50x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 'ToonNet via string'             | Serialize   | Employees | 10000 | 2,122.839 us |  18.0399 us |  8.0098 us |     baseline |         | 144.5313 | 144.5313 | 144.5313 |  2677591 B |                 |
| 'ToonNet SerializeToUtf8Bytes'   | Serialize   | Employees | 10000 | 1,981.370 us |   8.1758 us |  3.6301 us | 1.07x faster |   0.00x |  85.9375 |  85.9375 |  85.9375 |   893591 B |     2.996x less |
| 'ToonNet IBufferWriter (reused)' | Serialize   | Employees | 10000 | 1,883.276 us |  17.3312 us |  7.6951 us | 1.13x faster |   0.01x |        - |        - |        - |      864 B | 3,099.064x less |
| 'System.Text.Json UTF-8'         | Serialize   | Employees | 10000 | 2,116.201 us |  20.8341 us | 10.8966 us | 1.00x faster |   0.01x | 125.0000 | 125.0000 | 125.0000 |  1743320 B |     1.536x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 'ToonNet via string'             | Serialize   | Orders    | 100   |    27.535 us |   0.2347 us |  0.1042 us |     baseline |         |   8.4229 |   0.7019 |        - |    70816 B |                 |
| 'ToonNet SerializeToUtf8Bytes'   | Serialize   | Orders    | 100   |    26.366 us |   0.5905 us |  0.3088 us | 1.04x faster |   0.01x |   2.8381 |   0.2136 |        - |    24016 B |     2.949x less |
| 'ToonNet IBufferWriter (reused)' | Serialize   | Orders    | 100   |    25.273 us |   0.1625 us |  0.0722 us | 1.09x faster |   0.00x |   0.0610 |        - |        - |      576 B |   122.944x less |
| 'System.Text.Json UTF-8'         | Serialize   | Orders    | 100   |    25.507 us |   0.3737 us |  0.1954 us | 1.08x faster |   0.01x |   2.5024 |        - |        - |    21144 B |     3.349x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 'ToonNet via string'             | Serialize   | Orders    | 10000 | 3,176.703 us |  39.3761 us | 20.5944 us |     baseline |         | 156.2500 | 156.2500 | 156.2500 |  7226277 B |                 |
| 'ToonNet SerializeToUtf8Bytes'   | Serialize   | Orders    | 10000 | 2,756.468 us |  45.9955 us | 20.4223 us | 1.15x faster |   0.01x | 113.2813 | 113.2813 | 113.2813 |  2409890 B |     2.999x less |
| 'ToonNet IBufferWriter (reused)' | Serialize   | Orders    | 10000 | 2,568.296 us |  58.5035 us | 30.5984 us | 1.24x faster |   0.02x |        - |        - |        - |      728 B | 9,926.205x less |
| 'System.Text.Json UTF-8'         | Serialize   | Orders    | 10000 | 3,027.714 us |  47.2499 us | 20.9792 us | 1.05x faster |   0.01x |  97.6563 |  97.6563 |  97.6563 |  2149389 B |     3.362x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 'ToonNet via string'             | Serialize   | Products  | 100   |    26.747 us |   0.1923 us |  0.0854 us |     baseline |         |   2.0447 |   0.0305 |        - |    17128 B |                 |
| 'ToonNet SerializeToUtf8Bytes'   | Serialize   | Products  | 100   |    26.231 us |   0.3971 us |  0.1763 us | 1.02x faster |   0.01x |   0.8850 |        - |        - |     7432 B |      2.30x less |
| 'ToonNet IBufferWriter (reused)' | Serialize   | Products  | 100   |    26.080 us |   0.3153 us |  0.1649 us | 1.03x faster |   0.01x |   0.2747 |        - |        - |     2544 B |      6.73x less |
| 'System.Text.Json UTF-8'         | Serialize   | Products  | 100   |    27.314 us |   0.3031 us |  0.1585 us | 1.02x slower |   0.01x |   1.6785 |        - |        - |    14232 B |      1.20x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 'ToonNet via string'             | Serialize   | Products  | 10000 | 2,759.325 us |   4.6703 us |  2.0736 us |     baseline |         | 175.7813 | 175.7813 | 175.7813 |  1520086 B |                 |
| 'ToonNet SerializeToUtf8Bytes'   | Serialize   | Products  | 10000 | 2,649.250 us |   9.4948 us |  4.2157 us | 1.04x faster |   0.00x |  58.5938 |  58.5938 |  58.5938 |   508476 B |     2.989x less |
| 'ToonNet IBufferWriter (reused)' | Serialize   | Products  | 10000 | 2,613.359 us |  10.7179 us |  4.7588 us | 1.06x faster |   0.00x |        - |        - |        - |     2664 B |   570.603x less |
| 'System.Text.Json UTF-8'         | Serialize   | Products  | 10000 | 2,802.335 us |  14.2035 us |  6.3064 us | 1.02x slower |   0.00x | 187.5000 | 187.5000 | 187.5000 |  1417425 B |     1.072x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 'ToonNet via string'             | Serialize   | Servers   | 100   |     9.143 us |   0.1730 us |  0.0768 us |     baseline |         |   1.8311 |   0.0458 |        - |    15360 B |                 |
| 'ToonNet SerializeToUtf8Bytes'   | Serialize   | Servers   | 100   |     8.735 us |   0.0680 us |  0.0302 us | 1.05x faster |   0.01x |   0.8392 |        - |        - |     7120 B |      2.16x less |
| 'ToonNet IBufferWriter (reused)' | Serialize   | Servers   | 100   |     8.555 us |   0.1884 us |  0.0986 us | 1.07x faster |   0.01x |   0.3510 |        - |        - |     2960 B |      5.19x less |
| 'System.Text.Json UTF-8'         | Serialize   | Servers   | 100   |     8.210 us |   0.0331 us |  0.0147 us | 1.11x faster |   0.01x |   0.9460 |        - |        - |     8040 B |      1.91x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 'ToonNet via string'             | Serialize   | Servers   | 10000 |   947.502 us |   5.7348 us |  2.0451 us |     baseline |         | 248.0469 | 248.0469 | 248.0469 |  1495630 B |                 |
| 'ToonNet SerializeToUtf8Bytes'   | Serialize   | Servers   | 10000 |   890.604 us |  20.9818 us |  9.3160 us | 1.06x faster |   0.01x | 122.0703 | 122.0703 | 122.0703 |   606378 B |      2.47x less |
| 'ToonNet IBufferWriter (reused)' | Serialize   | Servers   | 10000 |   847.923 us |   6.1432 us |  2.7276 us | 1.12x faster |   0.00x |  33.2031 |  33.2031 |  33.2031 |   161769 B |      9.25x less |
| 'System.Text.Json UTF-8'         | Serialize   | Servers   | 10000 |   915.475 us |   8.4686 us |  3.7601 us | 1.03x faster |   0.00x | 164.0625 | 164.0625 | 164.0625 |   805851 B |      1.86x less |
