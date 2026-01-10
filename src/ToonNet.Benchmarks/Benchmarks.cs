using BenchmarkDotNet.Attributes;
using ToonNet.Core.Serialization;
using ToonNet.Benchmarks.Models;

namespace ToonNet.Benchmarks;

/// <summary>
/// Benchmarks for simple model.
/// </summary>
[SimpleJob(warmupCount: 3)]
[MemoryDiagnoser]
public class SimpleBenchmarks
{
    private SimpleBenchmarkModel _model = null!;

    [GlobalSetup]
    public void Setup()
    {
        _model = new SimpleBenchmarkModel { Name = "Test", Age = 30, Email = "test@example.com", Score = 95.5, IsActive = true };
    }

    [Benchmark]
    public global::ToonNet.Core.Models.ToonDocument SerializeGenerated() => SimpleBenchmarkModel.Serialize(_model);

    [Benchmark]
    public string SerializeReflection() => ToonSerializer.Serialize(_model);

    [Benchmark]
    public SimpleBenchmarkModel DeserializeGenerated() => SimpleBenchmarkModel.Deserialize(SimpleBenchmarkModel.Serialize(_model));

    [Benchmark]
    public SimpleBenchmarkModel? DeserializeReflection()
    {
        var str = ToonSerializer.Serialize(_model);
        return ToonSerializer.Deserialize<SimpleBenchmarkModel>(str);
    }
}

/// <summary>
/// Benchmarks for medium model.
/// </summary>
[SimpleJob(warmupCount: 3)]
[MemoryDiagnoser]
public class MediumBenchmarks
{
    private MediumBenchmarkModel _model = null!;

    [GlobalSetup]
    public void Setup()
    {
        _model = new MediumBenchmarkModel
        {
            FirstName = "John", LastName = "Doe", Age = 30, Email = "john@example.com",
            Phone = "555-1234", Salary = 75000.50m, Rating = 4.5, IsManager = false,
            DepartmentId = 5, EmployeeId = 123456789
        };
    }

    [Benchmark]
    public global::ToonNet.Core.Models.ToonDocument SerializeGenerated() => MediumBenchmarkModel.Serialize(_model);

    [Benchmark]
    public string SerializeReflection() => ToonSerializer.Serialize(_model);

    [Benchmark]
    public MediumBenchmarkModel DeserializeGenerated() => MediumBenchmarkModel.Deserialize(MediumBenchmarkModel.Serialize(_model));

    [Benchmark]
    public MediumBenchmarkModel? DeserializeReflection()
    {
        var str = ToonSerializer.Serialize(_model);
        return ToonSerializer.Deserialize<MediumBenchmarkModel>(str);
    }
}

/// <summary>
/// Benchmarks for complex model.
/// </summary>
[SimpleJob(warmupCount: 3)]
[MemoryDiagnoser]
public class ComplexBenchmarks
{
    private ComplexBenchmarkModel _model = null!;

    [GlobalSetup]
    public void Setup()
    {
        _model = new ComplexBenchmarkModel
        {
            CompanyName = "TechCorp", Address = "123 Main St", City = "Springfield", State = "IL",
            ZipCode = "62701", Phone = "217-555-1234", Website = "https://techcorp.com", FoundedYear = 1995,
            Revenue = 5000000.00m, EmployeeCount = 250, MarketShare = 12.5f, Status = 1,
            SectorCode = 10, RegistrationId = 987654321, IsPublic = true
        };
    }

    [Benchmark]
    public global::ToonNet.Core.Models.ToonDocument SerializeGenerated() => ComplexBenchmarkModel.Serialize(_model);

    [Benchmark]
    public string SerializeReflection() => ToonSerializer.Serialize(_model);

    [Benchmark]
    public ComplexBenchmarkModel DeserializeGenerated() => ComplexBenchmarkModel.Deserialize(ComplexBenchmarkModel.Serialize(_model));

    [Benchmark]
    public ComplexBenchmarkModel? DeserializeReflection()
    {
        var str = ToonSerializer.Serialize(_model);
        return ToonSerializer.Deserialize<ComplexBenchmarkModel>(str);
    }
}
