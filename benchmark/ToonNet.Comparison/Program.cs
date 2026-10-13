using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using ToonNet.Comparison;

// `parity [documents]` checks output parity with the official library (spec fixtures, random documents, typed data);
// anything else runs the benchmarks, e.g. `--filter "*"`.
if (args.Length > 0 && args[0] == "parity")
{
    return Parity.Report(args.Length > 1 ? int.Parse(args[1]) : 10_000) ? 0 : 1;
}

var config = DefaultConfig.Instance.WithSummaryStyle(SummaryStyle.Default.WithRatioStyle(RatioStyle.Trend));
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
return 0;
