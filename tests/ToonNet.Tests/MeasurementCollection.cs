namespace ToonNet.Tests;

/// <summary>
///     Tests that measure the process (live memory, elapsed time) run alone, after the parallel ones: other tests'
///     allocations would count as theirs.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class MeasurementCollection
{
    public const string Name = "Process measurements";
}
