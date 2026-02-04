using BenchmarkDotNet.Running;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Reports;

var config = DefaultConfig.Instance
    .WithSummaryStyle(SummaryStyle.Default.WithRatioStyle(RatioStyle.Trend));

// Run only ArrayPool optimization benchmarks
var summary = BenchmarkRunner.Run<ToonNet.Benchmarks.ArrayPoolOptimizationBenchmarks>(config);

Console.WriteLine("\n✅ Benchmark completed!");
Console.WriteLine($"Results saved to: {summary.ResultsDirectoryPath}");