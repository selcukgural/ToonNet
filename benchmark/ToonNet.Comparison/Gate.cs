using System.Diagnostics;
using System.Text.Json;
using ToonNet.Core.Models;
using ToonNet.Core.Serialization;

namespace ToonNet.Comparison;

/// <summary>
///     A quick local performance check: <c>gate --report file</c> measures every scenario, <c>gate --compare base head</c>
///     fails when the head is slower or allocates more than the base, both measured on the same machine.
/// </summary>
/// <remarks>
///     Times vary from run to run, so a scenario's time is measured as a ratio to System.Text.Json
///     doing the same work, interleaved in the same process: both are affected alike by the machine. Run it with
///     <c>DOTNET_TieredCompilation=0</c>, so the code is compiled once, fully optimized, instead of at varying times. Allocated bytes per
///     operation are exact; the smallest of several rounds is kept, so a pool that had to allocate once does not count.
/// </remarks>
public static class Gate
{
    /// <summary>A time ratio worse than this (head / base) fails the gate.</summary>
    private const double MaxSlowdown = 1.15;

    /// <summary>A time ratio worse than this is reported as a warning.</summary>
    private const double WarnSlowdown = 1.07;

    /// <summary>More allocation than base × this + <see cref="AllocationSlack"/> fails the gate.</summary>
    private const double MaxAllocationGrowth = 1.03;

    private const double AllocationSlack = 512;

    private const int Rounds = 11;

    /// <summary>The measured time of a batch, so timer resolution and call overhead do not matter.</summary>
    private static readonly TimeSpan BatchTime = TimeSpan.FromMilliseconds(25);

    public sealed record Metric(string Name, double TimeRatio, double BytesPerOperation);

    private sealed record Scenario(string Name, Action Run, Action Reference);

    public static int Run(string[] args)
    {
        System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

        switch (args)
        {
            case ["gate", "--report", var path]:
                var metrics = Measure();
                File.WriteAllText(path, JsonSerializer.Serialize(metrics, new JsonSerializerOptions { WriteIndented = true }));
                Console.WriteLine($"Wrote {metrics.Count} metrics to {path}");
                return 0;
            case ["gate", "--compare", var basePath, var headPath]:
                return Compare(Load(basePath), Load(headPath));
            default:
                Console.Error.WriteLine("Usage: gate --report <file> | gate --compare <base file> <head file>");
                return 2;
        }

        static List<Metric> Load(string path) => JsonSerializer.Deserialize<List<Metric>>(File.ReadAllText(path))!;
    }

    private static List<Metric> Measure()
    {
        var results = new List<Metric>();

        foreach (var scenario in Scenarios())
        {
            var metric = Measure(scenario);
            results.Add(metric);
            Console.WriteLine($"{metric.Name,-44} {metric.TimeRatio,6:F3}x STJ {metric.BytesPerOperation,12:N0} B/op");
        }

        return results;
    }

    private static IEnumerable<Scenario> Scenarios()
    {
        foreach (var count in new[] { 100, 10_000 })
        {
            foreach (var scenario in For("Employees", count, Datasets.Employees(count), ToonTypeInfo.CreateList(Employee.ToonTypeInfo)))
            {
                yield return scenario;
            }

            foreach (var scenario in For("Orders", count, Datasets.Orders(count), ToonTypeInfo.CreateList(Order.ToonTypeInfo)))
            {
                yield return scenario;
            }

            foreach (var scenario in For("Products", count, Datasets.Products(count), ToonTypeInfo.CreateList(Product.ToonTypeInfo)))
            {
                yield return scenario;
            }

            foreach (var scenario in For("Servers", count, Datasets.Servers(count), ToonTypeInfo.CreateDictionary(Server.ToonTypeInfo)))
            {
                yield return scenario;
            }
        }
    }

    private static IEnumerable<Scenario> For<T>(string data, int count, T value, ToonTypeInfo<T> typeInfo)
    {
        var reflection = new ToonSerializerOptions { UseGeneratedCode = false };
        var withConverter = new ToonSerializerOptions();
        withConverter.AddConverter(new UnusedConverter());

        var text = ToonSerializer.Serialize(value);
        var utf8 = ToonSerializer.SerializeToUtf8Bytes(value);
        var json = JsonSerializer.Serialize(value);
        var name = $"{data} {count}";

        Action serializeJson = () => JsonSerializer.Serialize(value);
        Action deserializeJson = () => JsonSerializer.Deserialize<T>(json);

        yield return new Scenario($"{name} serialize", () => ToonSerializer.Serialize(value), serializeJson);
        yield return new Scenario($"{name} serialize reflection", () => ToonSerializer.Serialize(value, reflection), serializeJson);
        yield return new Scenario($"{name} serialize type info", () => ToonSerializer.Serialize(value, typeInfo), serializeJson);
        yield return new Scenario($"{name} serialize utf-8", () => ToonSerializer.SerializeToUtf8Bytes(value), serializeJson);
        yield return new Scenario($"{name} serialize converter", () => ToonSerializer.Serialize(value, withConverter), serializeJson);
        yield return new Scenario($"{name} deserialize", () => ToonSerializer.Deserialize<T>(text), deserializeJson);
        yield return new Scenario($"{name} deserialize reflection", () => ToonSerializer.Deserialize<T>(text, reflection), deserializeJson);
        yield return new Scenario($"{name} deserialize type info", () => ToonSerializer.Deserialize(text, typeInfo), deserializeJson);
        yield return new Scenario($"{name} deserialize utf-8", () => ToonSerializer.Deserialize<T>(utf8), deserializeJson);
        yield return new Scenario($"{name} deserialize converter", () => ToonSerializer.Deserialize<T>(text, withConverter), deserializeJson);
    }

    private static Metric Measure(Scenario scenario)
    {
        // Warm up both (tiered compilation), then size the batches
        var operations = Calibrate(scenario.Run);
        var referenceOperations = Calibrate(scenario.Reference);

        var times = new double[Rounds];
        var referenceTimes = new double[Rounds];
        var bytes = double.MaxValue;

        for (var round = 0; round < Rounds; round++)
        {
            // Each batch starts on a clean heap, so collections left over from the other batch do not land in it
            GC.Collect();
            var allocated = GC.GetAllocatedBytesForCurrentThread();
            times[round] = Time(scenario.Run, operations);
            bytes = Math.Min(bytes, (double)(GC.GetAllocatedBytesForCurrentThread() - allocated) / operations);

            GC.Collect();
            referenceTimes[round] = Time(scenario.Reference, referenceOperations);
        }

        // The best round of each is the least disturbed by the machine
        return new Metric(scenario.Name, times.Min() / referenceTimes.Min(), Math.Round(bytes));
    }

    private static int Calibrate(Action action)
    {
        var stopwatch = Stopwatch.StartNew();
        var operations = 0;

        while (stopwatch.Elapsed < TimeSpan.FromMilliseconds(200) || operations < 5)
        {
            action();
            operations++;
        }

        var perOperation = stopwatch.Elapsed / operations;
        return Math.Max(1, (int)(BatchTime / perOperation));
    }

    /// <returns>The time per operation in microseconds.</returns>
    private static double Time(Action action, int operations)
    {
        var stopwatch = Stopwatch.StartNew();

        for (var i = 0; i < operations; i++)
        {
            action();
        }

        return stopwatch.Elapsed.TotalMicroseconds / operations;
    }

    private static int Compare(List<Metric> baseline, List<Metric> head)
    {
        var byName = baseline.ToDictionary(m => m.Name);
        var failures = 0;
        var lines = new List<string>
        {
            "| Scenario | Time vs STJ (base → head) | Change | Allocated B/op (base → head) | Change | |",
            "|---|---:|---:|---:|---:|---|"
        };

        foreach (var metric in head)
        {
            if (!byName.TryGetValue(metric.Name, out var old))
            {
                lines.Add($"| {metric.Name} | {metric.TimeRatio:F3} (new) | | {metric.BytesPerOperation:N0} (new) | | |");
                continue;
            }

            var slowdown = metric.TimeRatio / old.TimeRatio;
            var allocationLimit = old.BytesPerOperation * MaxAllocationGrowth + AllocationSlack;
            var allocationChange = old.BytesPerOperation == 0 ? 0 : metric.BytesPerOperation / old.BytesPerOperation - 1;

            var status = slowdown > MaxSlowdown || metric.BytesPerOperation > allocationLimit ? "❌"
                : slowdown > WarnSlowdown ? "⚠️"
                : "";

            if (status == "❌")
            {
                failures++;
            }

            lines.Add($"| {metric.Name} | {old.TimeRatio:F3} → {metric.TimeRatio:F3} | {slowdown - 1:+0.0%;-0.0%} | " +
                      $"{old.BytesPerOperation:N0} → {metric.BytesPerOperation:N0} | {allocationChange:+0.0%;-0.0%} | {status} |");
        }

        Console.WriteLine(string.Join(Environment.NewLine, lines));
        Console.WriteLine();
        Console.WriteLine(failures == 0
            ? "No regressions."
            : $"{failures} scenario(s) regressed: more than {MaxSlowdown - 1:0%} slower relative to System.Text.Json, or more than " +
              $"{MaxAllocationGrowth - 1:0%} (+{AllocationSlack} B) more allocation.");

        return failures == 0 ? 0 : 1;
    }

    /// <summary>A converter for a type the models do not use: generated code stays on.</summary>
    private sealed class UnusedConverter : ToonConverter<Uri>
    {
        public override ToonValue Write(Uri? value, ToonSerializerOptions options) => new ToonString(value!.ToString());

        public override Uri Read(ToonValue value, ToonSerializerOptions options) => new(((ToonString)value).Value);
    }
}
