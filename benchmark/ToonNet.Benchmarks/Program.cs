using BenchmarkDotNet.Running;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Reports;

var config = DefaultConfig.Instance
    .WithSummaryStyle(SummaryStyle.Default.WithRatioStyle(RatioStyle.Trend));

// Without arguments BenchmarkDotNet asks which benchmark classes to run; pass e.g. `--filter "*ParserOnly*"`
// to select them from the command line, or `--filter "*"` to run all of them.
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
