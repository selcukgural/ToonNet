# ToonNet vs Toon.Format – benchmark results

Measured with `dotnet run -c Release -- --filter "*" --launchCount 1 --warmupCount 3 --iterationCount 8` in this
directory on 2026-10-14 (one run for both tables).

```
BenchmarkDotNet v0.15.8, macOS Tahoe 26.6.2 (25G83) [Darwin 25.6.0]
Apple M3 Max, 1 CPU, 16 logical and 16 physical cores
.NET SDK 10.0.105, .NET 10.0.5 (Arm64 RyuJIT armv8.0-a)
Toon.Format 1.0.0, ToonNet 3.3.0
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
| Toon.Format            | Deserialize | Employees | 100   |    136.486 us |     0.9360 us |   0.4156 us |      baseline |         |    42.9688 |   11.7188 |         - |   350.97 KB |             |
| ToonNet                | Deserialize | Employees | 100   |     47.278 us |     0.7101 us |   0.3714 us |  2.89x faster |   0.02x |     3.3569 |    0.0610 |         - |    27.51 KB | 12.76x less |
| 'ToonNet (reflection)' | Deserialize | Employees | 100   |     52.785 us |     0.9370 us |   0.4901 us |  2.59x faster |   0.02x |     3.3569 |    0.2441 |         - |    27.48 KB | 12.77x less |
| 'ToonNet (type info)'  | Deserialize | Employees | 100   |     45.591 us |     0.6163 us |   0.2737 us |  2.99x faster |   0.02x |     3.3569 |    0.2441 |         - |    27.63 KB | 12.70x less |
| System.Text.Json       | Deserialize | Employees | 100   |     33.383 us |     0.8438 us |   0.4413 us |  4.09x faster |   0.05x |     3.0518 |    0.1831 |         - |    25.18 KB | 13.94x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Toon.Format            | Deserialize | Employees | 10000 | 31,605.594 us |   335.2886 us | 148.8702 us |      baseline |         |  5000.0000 | 2281.2500 |  843.7500 |    35153 KB |             |
| ToonNet                | Deserialize | Employees | 10000 |  5,064.599 us |    41.0015 us |  18.2049 us |  6.24x faster |   0.03x |   281.2500 |  140.6250 |         - |  2347.82 KB | 14.97x less |
| 'ToonNet (reflection)' | Deserialize | Employees | 10000 |  5,421.945 us |    69.2631 us |  36.2259 us |  5.83x faster |   0.04x |   281.2500 |  140.6250 |         - |  2347.79 KB | 14.97x less |
| 'ToonNet (type info)'  | Deserialize | Employees | 10000 |  4,895.136 us |    17.9604 us |   7.9745 us |  6.46x faster |   0.03x |   281.2500 |  140.6250 |         - |  2347.95 KB | 14.97x less |
| System.Text.Json       | Deserialize | Employees | 10000 |  4,173.556 us |    59.7788 us |  31.2655 us |  7.57x faster |   0.06x |   328.1250 |  203.1250 |   78.1250 |  2522.68 KB | 13.93x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Toon.Format            | Deserialize | Orders    | 100   |    246.003 us |     3.0122 us |   1.3374 us |      baseline |         |    99.1211 |   38.5742 |         - |   809.82 KB |             |
| ToonNet                | Deserialize | Orders    | 100   |     80.994 us |     1.8854 us |   0.9861 us |  3.04x faster |   0.04x |     7.0801 |    0.9766 |         - |    58.69 KB | 13.80x less |
| 'ToonNet (reflection)' | Deserialize | Orders    | 100   |    101.444 us |     1.2297 us |   0.5460 us |  2.43x faster |   0.02x |     7.9346 |    0.9766 |         - |    64.84 KB | 12.49x less |
| 'ToonNet (type info)'  | Deserialize | Orders    | 100   |     80.860 us |     1.8559 us |   0.9707 us |  3.04x faster |   0.04x |     7.9346 |    0.9766 |         - |       65 KB | 12.46x less |
| System.Text.Json       | Deserialize | Orders    | 100   |     50.925 us |     0.5796 us |   0.2573 us |  4.83x faster |   0.03x |     7.1411 |    0.9766 |         - |    58.77 KB | 13.78x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Toon.Format            | Deserialize | Orders    | 10000 | 64,695.367 us | 1,957.9255 us | 869.3311 us |      baseline |         | 10333.3333 | 4666.6667 | 1333.3333 | 80185.86 KB |             |
| ToonNet                | Deserialize | Orders    | 10000 |  9,286.901 us |   155.4966 us |  81.3277 us |  6.97x faster |   0.10x |   671.8750 |  328.1250 |         - |  5550.09 KB | 14.45x less |
| 'ToonNet (reflection)' | Deserialize | Orders    | 10000 | 10,751.520 us |    85.7317 us |  38.0654 us |  6.02x faster |   0.08x |   671.8750 |  328.1250 |         - |  5550.06 KB | 14.45x less |
| 'ToonNet (type info)'  | Deserialize | Orders    | 10000 |  9,078.050 us |    29.1452 us |  10.3934 us |  7.13x faster |   0.09x |   671.8750 |  328.1250 |         - |  5550.22 KB | 14.45x less |
| System.Text.Json       | Deserialize | Orders    | 10000 |  8,443.389 us |   110.4854 us |  49.0562 us |  7.66x faster |   0.10x |   812.5000 |  500.0000 |  187.5000 |  5882.29 KB | 13.63x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Toon.Format            | Deserialize | Products  | 100   |    142.545 us |     3.2637 us |   1.4491 us |      baseline |         |    36.1328 |    9.7656 |         - |   295.87 KB |             |
| ToonNet                | Deserialize | Products  | 100   |     41.281 us |     0.8588 us |   0.4491 us |  3.45x faster |   0.05x |     4.6387 |    0.3662 |         - |    38.33 KB |  7.72x less |
| 'ToonNet (reflection)' | Deserialize | Products  | 100   |     52.623 us |     0.9236 us |   0.4831 us |  2.71x faster |   0.03x |     4.6387 |    0.4272 |         - |     38.3 KB |  7.73x less |
| 'ToonNet (type info)'  | Deserialize | Products  | 100   |     39.534 us |     0.7978 us |   0.4173 us |  3.61x faster |   0.05x |     4.6997 |    0.3662 |         - |    38.45 KB |  7.69x less |
| System.Text.Json       | Deserialize | Products  | 100   |     42.614 us |     0.9328 us |   0.4879 us |  3.35x faster |   0.05x |     4.3945 |    0.3662 |         - |    36.12 KB |  8.19x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Toon.Format            | Deserialize | Products  | 10000 | 30,679.784 us |   295.5782 us | 131.2386 us |      baseline |         |  4333.3333 | 2133.3333 |  866.6667 | 29471.59 KB |             |
| ToonNet                | Deserialize | Products  | 10000 |  4,803.920 us |    76.3819 us |  39.9492 us |  6.39x faster |   0.06x |   414.0625 |  203.1250 |         - |  3441.45 KB |  8.56x less |
| 'ToonNet (reflection)' | Deserialize | Products  | 10000 |  5,579.127 us |    51.9783 us |  23.0787 us |  5.50x faster |   0.03x |   414.0625 |  203.1250 |         - |  3441.42 KB |  8.56x less |
| 'ToonNet (type info)'  | Deserialize | Products  | 10000 |  4,418.049 us |    14.7206 us |   6.5360 us |  6.94x faster |   0.03x |   414.0625 |  203.1250 |         - |  3441.58 KB |  8.56x less |
| System.Text.Json       | Deserialize | Products  | 10000 |  6,313.782 us |    60.5327 us |  26.8769 us |  4.86x faster |   0.03x |   484.3750 |  218.7500 |   78.1250 |   3616.3 KB |  8.15x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Toon.Format            | Deserialize | Servers   | 100   |     65.374 us |     0.5952 us |   0.3113 us |      baseline |         |    26.8555 |    5.8594 |         - |    223.3 KB |             |
| ToonNet                | Deserialize | Servers   | 100   |     20.701 us |     0.5962 us |   0.2647 us |  3.16x faster |   0.04x |     4.3640 |    0.4272 |         - |    35.84 KB |  6.23x less |
| 'ToonNet (reflection)' | Deserialize | Servers   | 100   |     24.455 us |     0.3299 us |   0.1465 us |  2.67x faster |   0.02x |     4.3640 |    0.3357 |         - |    35.81 KB |  6.24x less |
| 'ToonNet (type info)'  | Deserialize | Servers   | 100   |     19.414 us |     0.4442 us |   0.2323 us |  3.37x faster |   0.04x |     4.3945 |    0.3662 |         - |    35.97 KB |  6.21x less |
| System.Text.Json       | Deserialize | Servers   | 100   |     17.659 us |     0.1777 us |   0.0789 us |  3.70x faster |   0.02x |     3.1738 |    0.2441 |         - |    26.04 KB |  8.58x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Toon.Format            | Deserialize | Servers   | 10000 | 15,297.963 us |   193.0773 us |  85.7275 us |      baseline |         |  2843.7500 | 1375.0000 |  531.2500 | 22392.54 KB |             |
| ToonNet                | Deserialize | Servers   | 10000 |  3,493.406 us |    42.2039 us |  22.0735 us |  4.38x faster |   0.03x |   402.3438 |  257.8125 |  156.2500 |  3211.09 KB |  6.97x less |
| 'ToonNet (reflection)' | Deserialize | Servers   | 10000 |  3,721.726 us |    96.8412 us |  50.6498 us |  4.11x faster |   0.06x |   398.4375 |  253.9063 |  156.2500 |  3211.04 KB |  6.97x less |
| 'ToonNet (type info)'  | Deserialize | Servers   | 10000 |  3,261.711 us |    68.3067 us |  35.7257 us |  4.69x faster |   0.05x |   402.3438 |  257.8125 |  156.2500 |  3211.21 KB |  6.97x less |
| System.Text.Json       | Deserialize | Servers   | 10000 |  2,754.525 us |   104.6329 us |  46.4577 us |  5.56x faster |   0.09x |   273.4375 |  160.1563 |   74.2188 |   2551.2 KB |  8.78x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Toon.Format            | Serialize   | Employees | 100   |    231.723 us |     4.5684 us |   2.3894 us |      baseline |         |    62.5000 |   27.3438 |         - |   525.56 KB |             |
| ToonNet                | Serialize   | Employees | 100   |     18.235 us |     0.0571 us |   0.0254 us | 12.71x faster |   0.12x |     2.0447 |         - |         - |    16.81 KB | 31.26x less |
| 'ToonNet (reflection)' | Serialize   | Employees | 100   |     25.210 us |     0.3197 us |   0.1419 us |  9.19x faster |   0.10x |     2.0447 |         - |         - |    16.81 KB | 31.26x less |
| 'ToonNet (type info)'  | Serialize   | Employees | 100   |     18.345 us |     0.3506 us |   0.1834 us | 12.63x faster |   0.17x |     2.0447 |         - |         - |    16.94 KB | 31.03x less |
| System.Text.Json       | Serialize   | Employees | 100   |     20.948 us |     0.2009 us |   0.0716 us | 11.06x faster |   0.11x |     4.0283 |         - |         - |    33.19 KB | 15.84x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Toon.Format            | Serialize   | Employees | 10000 | 45,577.304 us |   810.9571 us | 360.0700 us |      baseline |         |  6818.1818 | 3181.8182 | 1000.0000 | 53017.27 KB |             |
| ToonNet                | Serialize   | Employees | 10000 |  2,009.748 us |    58.0019 us |  30.3361 us | 22.68x faster |   0.36x |   152.3438 |  152.3438 |  152.3438 |  1744.23 KB | 30.40x less |
| 'ToonNet (reflection)' | Serialize   | Employees | 10000 |  2,643.051 us |    31.9413 us |  14.1821 us | 17.24x faster |   0.15x |   152.3438 |  152.3438 |  152.3438 |  1744.29 KB | 30.39x less |
| 'ToonNet (type info)'  | Serialize   | Employees | 10000 |  2,004.806 us |    15.4826 us |   8.0977 us | 22.73x faster |   0.19x |   152.3438 |  152.3438 |  152.3438 |  1744.33 KB | 30.39x less |
| System.Text.Json       | Serialize   | Employees | 10000 |  2,220.157 us |     6.0175 us |   2.1459 us | 20.53x faster |   0.15x |   242.1875 |  242.1875 |  242.1875 |  3404.85 KB | 15.57x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Toon.Format            | Serialize   | Orders    | 100   |    322.804 us |     2.0997 us |   1.0982 us |      baseline |         |   105.4688 |   35.1563 |         - |   876.35 KB |             |
| ToonNet                | Serialize   | Orders    | 100   |     25.713 us |     0.0853 us |   0.0304 us | 12.55x faster |   0.04x |     5.6152 |         - |         - |    46.14 KB | 18.99x less |
| 'ToonNet (reflection)' | Serialize   | Orders    | 100   |     38.863 us |     0.3872 us |   0.1719 us |  8.31x faster |   0.04x |     5.6152 |         - |         - |    46.14 KB | 18.99x less |
| 'ToonNet (type info)'  | Serialize   | Orders    | 100   |     25.725 us |     0.2684 us |   0.1192 us | 12.55x faster |   0.07x |     5.6458 |         - |         - |    46.27 KB | 18.94x less |
| System.Text.Json       | Serialize   | Orders    | 100   |     27.179 us |     0.4847 us |   0.2535 us | 11.88x faster |   0.11x |     4.9744 |         - |         - |    40.96 KB | 21.39x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Toon.Format            | Serialize   | Orders    | 10000 | 73,857.534 us |   854.8093 us | 379.5406 us |      baseline |         | 10333.3333 | 4000.0000 | 1000.0000 | 87005.89 KB |             |
| ToonNet                | Serialize   | Orders    | 10000 |  2,746.522 us |     8.6882 us |   3.0983 us | 26.89x faster |   0.13x |   156.2500 |  156.2500 |  156.2500 |  4705.62 KB | 18.49x less |
| 'ToonNet (reflection)' | Serialize   | Orders    | 10000 |  3,929.771 us |     8.8939 us |   3.9490 us | 18.79x faster |   0.09x |   140.6250 |  140.6250 |  140.6250 |  4705.45 KB | 18.49x less |
| 'ToonNet (type info)'  | Serialize   | Orders    | 10000 |  2,734.411 us |     9.1623 us |   3.2673 us | 27.01x faster |   0.13x |   156.2500 |  156.2500 |  156.2500 |  4705.76 KB | 18.49x less |
| System.Text.Json       | Serialize   | Orders    | 10000 |  3,098.612 us |    24.9444 us |   8.8954 us | 23.84x faster |   0.13x |    62.5000 |   62.5000 |   62.5000 |  4196.44 KB | 20.73x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Toon.Format            | Serialize   | Products  | 100   |    256.985 us |     0.7058 us |   0.3134 us |      baseline |         |    66.4063 |   21.4844 |         - |   545.25 KB |             |
| ToonNet                | Serialize   | Products  | 100   |     26.246 us |     0.6211 us |   0.2758 us |  9.79x faster |   0.10x |     1.4343 |         - |         - |    11.84 KB | 46.07x less |
| 'ToonNet (reflection)' | Serialize   | Products  | 100   |     33.259 us |     0.1116 us |   0.0398 us |  7.73x faster |   0.01x |     1.3428 |         - |         - |    11.13 KB | 48.98x less |
| 'ToonNet (type info)'  | Serialize   | Products  | 100   |     26.126 us |     0.4398 us |   0.1953 us |  9.84x faster |   0.07x |     1.4343 |         - |         - |    11.96 KB | 45.59x less |
| System.Text.Json       | Serialize   | Products  | 100   |     27.807 us |     0.4570 us |   0.2029 us |  9.24x faster |   0.06x |     3.3264 |         - |         - |    27.45 KB | 19.86x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Toon.Format            | Serialize   | Products  | 10000 | 49,093.787 us |   916.4082 us | 406.8909 us |      baseline |         |  6750.0000 | 3000.0000 |  750.0000 | 54374.37 KB |             |
| ToonNet                | Serialize   | Products  | 10000 |  2,740.233 us |    60.3423 us |  31.5602 us | 17.92x faster |   0.24x |   101.5625 |  101.5625 |  101.5625 |   990.23 KB | 54.91x less |
| 'ToonNet (reflection)' | Serialize   | Products  | 10000 |  3,191.609 us |    74.0317 us |  38.7200 us | 15.38x faster |   0.21x |   101.5625 |  101.5625 |  101.5625 |   989.74 KB | 54.94x less |
| 'ToonNet (type info)'  | Serialize   | Products  | 10000 |  2,641.106 us |     9.9527 us |   3.5492 us | 18.59x faster |   0.15x |   101.5625 |  101.5625 |  101.5625 |   990.39 KB | 54.90x less |
| System.Text.Json       | Serialize   | Products  | 10000 |  2,982.597 us |    91.9553 us |  48.0944 us | 16.46x faster |   0.28x |   273.4375 |  273.4375 |  273.4375 |  2767.36 KB | 19.65x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Toon.Format            | Serialize   | Servers   | 100   |    108.866 us |     3.0506 us |   1.5955 us |      baseline |         |    34.1797 |    7.8125 |         - |   281.51 KB |             |
| ToonNet                | Serialize   | Servers   | 100   |      8.891 us |     0.0419 us |   0.0186 us | 12.24x faster |   0.17x |     1.3123 |         - |         - |    10.82 KB | 26.02x less |
| 'ToonNet (reflection)' | Serialize   | Servers   | 100   |      9.863 us |     0.0225 us |   0.0080 us | 11.04x faster |   0.15x |     1.2665 |         - |         - |    10.46 KB | 26.91x less |
| 'ToonNet (type info)'  | Serialize   | Servers   | 100   |      8.836 us |     0.1853 us |   0.0969 us | 12.32x faster |   0.21x |     1.3275 |         - |         - |    10.95 KB | 25.72x less |
| System.Text.Json       | Serialize   | Servers   | 100   |      8.870 us |     0.2272 us |   0.1188 us | 12.28x faster |   0.23x |     1.8768 |         - |         - |    15.37 KB | 18.32x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Toon.Format            | Serialize   | Servers   | 10000 | 21,468.020 us |   368.7584 us | 192.8678 us |      baseline |         |  3833.3333 | 1666.6667 |  666.6667 | 28252.72 KB |             |
| ToonNet                | Serialize   | Servers   | 10000 |    943.121 us |    16.8210 us |   8.7977 us | 22.76x faster |   0.28x |   147.4609 |  147.4609 |  147.4609 |  1025.92 KB | 27.54x less |
| 'ToonNet (reflection)' | Serialize   | Servers   | 10000 |    994.386 us |     3.1896 us |   1.4162 us | 21.59x faster |   0.19x |   146.4844 |  146.4844 |  146.4844 |  1025.86 KB | 27.54x less |
| 'ToonNet (type info)'  | Serialize   | Servers   | 10000 |    934.726 us |    14.3735 us |   7.5176 us | 22.97x faster |   0.26x |   147.4609 |  147.4609 |  147.4609 |   1026.1 KB | 27.53x less |
| System.Text.Json       | Serialize   | Servers   | 10000 |    930.244 us |    15.5993 us |   6.9262 us | 23.08x faster |   0.25x |   247.0703 |  247.0703 |  247.0703 |  1573.04 KB | 17.96x less |

In full runs a few Deserialize Orders 100 rows allocate about 6 KB more than the code does (here "ToonNet (reflection)"
and "ToonNet (type info)"); counted exactly with `gate --report` in this project, they allocate 60,064 B and 60,224 B per
operation, like "ToonNet".

## UTF-8 input and output

ToonNet through a string (`Encoding.UTF8.GetString` then `Deserialize`, `Serialize` then `Encoding.UTF8.GetBytes`)
against its UTF-8 overloads (`Deserialize(ReadOnlySpan<byte>)`, `SerializeToUtf8Bytes`, `Serialize` to a reused
`ArrayBufferWriter<byte>`), with System.Text.Json's UTF-8 methods for reference. Ratios are relative to going through a
string.

| Method                           | Categories  | Data      | Count | Mean         | Error       | StdDev      | Ratio        | RatioSD | Gen0     | Gen1     | Gen2     | Allocated  | Alloc Ratio     |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 'ToonNet via string'             | Deserialize | Employees | 100   |    49.835 us |   1.0241 us |   0.5356 us |     baseline |         |   5.3711 |   0.5493 |        - |    45096 B |                 |
| 'ToonNet UTF-8'                  | Deserialize | Employees | 100   |    47.783 us |   0.6694 us |   0.2972 us | 1.04x faster |   0.01x |   3.3569 |   0.2441 |        - |    28296 B |      1.59x less |
| 'System.Text.Json UTF-8'         | Deserialize | Employees | 100   |    32.646 us |   0.0925 us |   0.0411 us | 1.53x faster |   0.02x |   3.0518 |   0.1831 |        - |    25784 B |      1.75x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 'ToonNet via string'             | Deserialize | Employees | 10000 | 5,064.914 us |  42.4091 us |  18.8299 us |     baseline |         | 414.0625 | 265.6250 | 132.8125 |  4188533 B |                 |
| 'ToonNet UTF-8'                  | Deserialize | Employees | 10000 | 5,405.359 us |  55.8549 us |  29.2132 us | 1.07x slower |   0.01x | 281.2500 | 140.6250 |        - |  2404296 B |      1.74x less |
| 'System.Text.Json UTF-8'         | Deserialize | Employees | 10000 | 4,081.425 us |  22.5371 us |  10.0066 us | 1.24x faster |   0.01x | 328.1250 | 203.1250 |  78.1250 |  2583175 B |      1.62x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 'ToonNet via string'             | Deserialize | Orders    | 100   |    84.891 us |   0.4298 us |   0.1908 us |     baseline |         |  13.6719 |   2.6855 |        - |   114992 B |                 |
| 'ToonNet UTF-8'                  | Deserialize | Orders    | 100   |    81.332 us |   0.8261 us |   0.4320 us | 1.04x faster |   0.01x |   7.0801 |   0.8545 |        - |    60224 B |      1.91x less |
| 'System.Text.Json UTF-8'         | Deserialize | Orders    | 100   |    50.524 us |   1.4302 us |   0.6350 us | 1.68x faster |   0.02x |   7.1411 |   0.9766 |        - |    60184 B |      1.91x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 'ToonNet via string'             | Deserialize | Orders    | 10000 | 9,857.203 us | 194.3527 us | 101.6502 us |     baseline |         | 781.2500 | 437.5000 | 109.3750 | 10500483 B |                 |
| 'ToonNet UTF-8'                  | Deserialize | Orders    | 10000 | 9,052.054 us |  12.6543 us |   5.6186 us | 1.09x faster |   0.01x | 671.8750 | 328.1250 |        - |  5683424 B |      1.85x less |
| 'System.Text.Json UTF-8'         | Deserialize | Orders    | 10000 | 8,451.905 us | 243.5752 us | 127.3945 us | 1.17x faster |   0.02x | 812.5000 | 500.0000 | 187.5000 |  6023305 B |      1.74x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 'ToonNet via string'             | Deserialize | Products  | 100   |    45.348 us |   0.9630 us |   0.5037 us |     baseline |         |   5.8594 |   0.6104 |        - |    49120 B |                 |
| 'ToonNet UTF-8'                  | Deserialize | Products  | 100   |    43.173 us |   1.1360 us |   0.5942 us | 1.05x faster |   0.02x |   4.6997 |   0.3662 |        - |    39376 B |      1.25x less |
| 'System.Text.Json UTF-8'         | Deserialize | Products  | 100   |    44.536 us |   2.7830 us |   1.4555 us | 1.02x faster |   0.03x |   4.3945 |   0.3662 |        - |    36984 B |      1.33x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 'ToonNet via string'             | Deserialize | Products  | 10000 | 5,490.347 us | 134.9886 us |  70.6016 us |     baseline |         | 523.4375 | 312.5000 | 101.5625 |  4535167 B |                 |
| 'ToonNet UTF-8'                  | Deserialize | Products  | 10000 | 4,864.385 us |  99.6553 us |  52.1216 us | 1.13x faster |   0.02x | 414.0625 | 203.1250 |        - |  3524176 B |      1.29x less |
| 'System.Text.Json UTF-8'         | Deserialize | Products  | 10000 | 6,236.841 us | 114.6734 us |  50.9157 us | 1.14x slower |   0.02x | 484.3750 | 218.7500 |  78.1250 |  3703032 B |      1.22x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 'ToonNet via string'             | Deserialize | Servers   | 100   |    21.133 us |   0.5462 us |   0.2425 us |     baseline |         |   5.3711 |   0.4883 |        - |    45120 B |                 |
| 'ToonNet UTF-8'                  | Deserialize | Servers   | 100   |    21.263 us |   0.5778 us |   0.3022 us | 1.01x slower |   0.02x |   4.3945 |   0.3662 |        - |    36832 B |      1.23x less |
| 'System.Text.Json UTF-8'         | Deserialize | Servers   | 100   |    17.393 us |   0.2353 us |   0.1045 us | 1.22x faster |   0.01x |   3.1738 |   0.2441 |        - |    26664 B |      1.69x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 'ToonNet via string'             | Deserialize | Servers   | 10000 | 3,360.168 us |  69.8734 us |  31.0242 us |     baseline |         | 480.4688 | 332.0313 | 246.0938 |  4176530 B |                 |
| 'ToonNet UTF-8'                  | Deserialize | Servers   | 10000 | 3,667.057 us | 121.4065 us |  63.4979 us | 1.09x slower |   0.02x | 406.2500 | 253.9063 | 160.1563 |  3288224 B |      1.27x less |
| 'System.Text.Json UTF-8'         | Deserialize | Servers   | 10000 | 2,774.984 us |  32.3695 us |  14.3723 us | 1.21x faster |   0.01x | 339.8438 | 214.8438 | 128.9063 |  2612754 B |      1.60x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 'ToonNet via string'             | Serialize   | Employees | 100   |    19.015 us |   0.4432 us |   0.2318 us |     baseline |         |   3.0518 |   0.1526 |        - |    25760 B |                 |
| 'ToonNet SerializeToUtf8Bytes'   | Serialize   | Employees | 100   |    18.592 us |   0.5160 us |   0.2699 us | 1.02x faster |   0.02x |   1.0681 |   0.0305 |        - |     9008 B |      2.86x less |
| 'ToonNet IBufferWriter (reused)' | Serialize   | Employees | 100   |    18.102 us |   0.1799 us |   0.0799 us | 1.05x faster |   0.01x |   0.0610 |        - |        - |      592 B |     43.51x less |
| 'System.Text.Json UTF-8'         | Serialize   | Employees | 100   |    19.775 us |   0.2600 us |   0.1155 us | 1.04x slower |   0.01x |   2.0447 |        - |        - |    17160 B |      1.50x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 'ToonNet via string'             | Serialize   | Employees | 10000 | 2,107.507 us |   8.4978 us |   4.4445 us |     baseline |         | 144.5313 | 144.5313 | 144.5313 |  2677574 B |                 |
| 'ToonNet SerializeToUtf8Bytes'   | Serialize   | Employees | 10000 | 2,004.035 us |  46.4517 us |  24.2951 us | 1.05x faster |   0.01x |  85.9375 |  85.9375 |  85.9375 |   893591 B |     2.996x less |
| 'ToonNet IBufferWriter (reused)' | Serialize   | Employees | 10000 | 1,870.134 us |  35.5653 us |  18.6013 us | 1.13x faster |   0.01x |        - |        - |        - |      864 B | 3,099.044x less |
| 'System.Text.Json UTF-8'         | Serialize   | Employees | 10000 | 2,087.631 us |  41.3687 us |  18.3680 us | 1.01x faster |   0.01x | 125.0000 | 125.0000 | 125.0000 |  1743320 B |     1.536x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 'ToonNet via string'             | Serialize   | Orders    | 100   |    27.814 us |   0.5412 us |   0.2403 us |     baseline |         |   8.4229 |   0.7019 |        - |    70816 B |                 |
| 'ToonNet SerializeToUtf8Bytes'   | Serialize   | Orders    | 100   |    26.162 us |   0.0894 us |   0.0319 us | 1.06x faster |   0.01x |   2.8381 |   0.2136 |        - |    24016 B |     2.949x less |
| 'ToonNet IBufferWriter (reused)' | Serialize   | Orders    | 100   |    25.492 us |   0.5074 us |   0.2253 us | 1.09x faster |   0.01x |   0.0610 |        - |        - |      576 B |   122.944x less |
| 'System.Text.Json UTF-8'         | Serialize   | Orders    | 100   |    25.540 us |   0.6566 us |   0.3434 us | 1.09x faster |   0.02x |   2.5024 |        - |        - |    21144 B |     3.349x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 'ToonNet via string'             | Serialize   | Orders    | 10000 | 3,153.930 us |  11.3900 us |   5.0572 us |     baseline |         | 156.2500 | 156.2500 | 156.2500 |  7226277 B |                 |
| 'ToonNet SerializeToUtf8Bytes'   | Serialize   | Orders    | 10000 | 2,764.091 us |   6.5085 us |   2.3210 us | 1.14x faster |   0.00x | 113.2813 | 113.2813 | 113.2813 |  2409890 B |     2.999x less |
| 'ToonNet IBufferWriter (reused)' | Serialize   | Orders    | 10000 | 2,551.893 us |  16.8464 us |   7.4799 us | 1.24x faster |   0.00x |        - |        - |        - |      728 B | 9,926.205x less |
| 'System.Text.Json UTF-8'         | Serialize   | Orders    | 10000 | 2,753.683 us |  46.3980 us |  24.2671 us | 1.15x faster |   0.01x |  97.6563 |  97.6563 |  97.6563 |  2149389 B |     3.362x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 'ToonNet via string'             | Serialize   | Products  | 100   |    26.429 us |   0.2228 us |   0.0989 us |     baseline |         |   2.0447 |   0.0305 |        - |    17136 B |                 |
| 'ToonNet SerializeToUtf8Bytes'   | Serialize   | Products  | 100   |    26.338 us |   0.2490 us |   0.1303 us | 1.00x faster |   0.01x |   0.8850 |        - |        - |     7440 B |      2.30x less |
| 'ToonNet IBufferWriter (reused)' | Serialize   | Products  | 100   |    25.850 us |   0.1829 us |   0.0812 us | 1.02x faster |   0.00x |   0.2747 |        - |        - |     2552 B |      6.71x less |
| 'System.Text.Json UTF-8'         | Serialize   | Products  | 100   |    27.182 us |   0.2664 us |   0.1183 us | 1.03x slower |   0.01x |   1.6785 |        - |        - |    14232 B |      1.20x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 'ToonNet via string'             | Serialize   | Products  | 10000 | 2,809.077 us |  44.2642 us |  19.6536 us |     baseline |         | 175.7813 | 175.7813 | 175.7813 |  1520103 B |                 |
| 'ToonNet SerializeToUtf8Bytes'   | Serialize   | Products  | 10000 | 2,693.509 us |  34.8584 us |  15.4774 us | 1.04x faster |   0.01x |  58.5938 |  58.5938 |  58.5938 |   508470 B |     2.990x less |
| 'ToonNet IBufferWriter (reused)' | Serialize   | Products  | 10000 | 2,594.784 us |  14.0567 us |   5.0128 us | 1.08x faster |   0.01x |        - |        - |        - |     2672 B |   568.901x less |
| 'System.Text.Json UTF-8'         | Serialize   | Products  | 10000 | 2,852.238 us |  32.6730 us |  11.6515 us | 1.02x slower |   0.01x | 187.5000 | 187.5000 | 187.5000 |  1417454 B |     1.072x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 'ToonNet via string'             | Serialize   | Servers   | 100   |     9.258 us |   0.1376 us |   0.0611 us |     baseline |         |   1.8311 |   0.0305 |        - |    15368 B |                 |
| 'ToonNet SerializeToUtf8Bytes'   | Serialize   | Servers   | 100   |     8.947 us |   0.1735 us |   0.0770 us | 1.03x faster |   0.01x |   0.8392 |        - |        - |     7128 B |      2.16x less |
| 'ToonNet IBufferWriter (reused)' | Serialize   | Servers   | 100   |     8.722 us |   0.0911 us |   0.0477 us | 1.06x faster |   0.01x |   0.3510 |        - |        - |     2968 B |      5.18x less |
| 'System.Text.Json UTF-8'         | Serialize   | Servers   | 100   |     8.294 us |   0.0673 us |   0.0299 us | 1.12x faster |   0.01x |   0.9460 |        - |        - |     8040 B |      1.91x less |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 'ToonNet via string'             | Serialize   | Servers   | 10000 | 1,010.013 us |  23.1820 us |  12.1246 us |     baseline |         | 257.8125 | 257.8125 | 257.8125 |  1495691 B |                 |
| 'ToonNet SerializeToUtf8Bytes'   | Serialize   | Servers   | 10000 |   908.744 us |  13.9192 us |   7.2800 us | 1.11x faster |   0.02x | 122.0703 | 122.0703 | 122.0703 |   606395 B |      2.47x less |
| 'ToonNet IBufferWriter (reused)' | Serialize   | Servers   | 10000 |   893.949 us |  15.9394 us |   8.3366 us | 1.13x faster |   0.02x |  34.1797 |  34.1797 |  34.1797 |   161777 B |      9.25x less |
| 'System.Text.Json UTF-8'         | Serialize   | Servers   | 10000 |   893.703 us |   3.0096 us |   1.3363 us | 1.13x faster |   0.01x | 163.0859 | 163.0859 | 163.0859 |   805851 B |      1.86x less |
