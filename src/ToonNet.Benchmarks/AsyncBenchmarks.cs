using BenchmarkDotNet.Attributes;
using ToonNet.Benchmarks.Models;
using ToonNet.Core.Encoding;
using ToonNet.Core.Parsing;
using ToonNet.Core.Serialization;

namespace ToonNet.Benchmarks;

/// <summary>
///     Benchmarks for async operations.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class AsyncBenchmarks
{
    private string _smallDocument = null!;
    private string _mediumDocument = null!;
    private MediumBenchmarkModel _model = null!;
    
    private ToonParser _parser = null!;
    private ToonEncoder _encoder = null!;
    private string _tempFilePath = null!;

    [GlobalSetup]
    public void Setup()
    {
        _parser = new ToonParser();
        _encoder = new ToonEncoder();
        _tempFilePath = Path.GetTempFileName();
        
        _model = new MediumBenchmarkModel
        {
            FirstName = "John",
            LastName = "Doe",
            Age = 30,
            Email = "john@example.com",
            Phone = "555-1234",
            Salary = 75000.50m,
            Rating = 4.5,
            IsManager = false,
            DepartmentId = 5,
            EmployeeId = 123456789
        };
        
        // Small document (~1KB)
        var items = Enumerable.Range(0, 10).Select(i => new
        {
            Id = i,
            Name = $"User_{i}",
            Email = $"user{i}@example.com"
        });
        _smallDocument = ToonSerializer.Serialize(items);
        
        // Medium document (~10KB)
        var mediumItems = Enumerable.Range(0, 100).Select(i => new
        {
            Id = i,
            Name = $"User_{i}",
            Email = $"user{i}@example.com",
            Age = 20 + i,
            Score = 50.0 + i
        });
        _mediumDocument = ToonSerializer.Serialize(mediumItems);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (File.Exists(_tempFilePath))
        {
            File.Delete(_tempFilePath);
        }
    }

    [Benchmark]
    public async Task<string> SerializeAsync_Small()
    {
        return await ToonSerializer.SerializeAsync(_model);
    }

    [Benchmark]
    public async Task<MediumBenchmarkModel?> DeserializeAsync_Small()
    {
        return await ToonSerializer.DeserializeAsync<MediumBenchmarkModel>(_smallDocument);
    }

    [Benchmark]
    public async Task ParseAsync_Small()
    {
        await _parser.ParseAsync(_smallDocument);
    }

    [Benchmark]
    public async Task ParseAsync_Medium()
    {
        await _parser.ParseAsync(_mediumDocument);
    }

    [Benchmark]
    public async Task EncodeAsync_Small()
    {
        var doc = _parser.Parse(_smallDocument);
        await _encoder.EncodeAsync(doc);
    }

    [Benchmark]
    public async Task FileOperations_SerializeToFile()
    {
        await ToonSerializer.SerializeToFileAsync(_model, _tempFilePath);
    }

    [Benchmark]
    public async Task FileOperations_DeserializeFromFile()
    {
        await ToonSerializer.SerializeToFileAsync(_model, _tempFilePath);
        await ToonSerializer.DeserializeFromFileAsync<MediumBenchmarkModel>(_tempFilePath);
    }

    [Benchmark]
    public async Task StreamOperations_WriteAndRead()
    {
        using var stream = new MemoryStream();
        await ToonSerializer.SerializeToStreamAsync(_model, stream);
        stream.Position = 0;
        await ToonSerializer.DeserializeFromStreamAsync<MediumBenchmarkModel>(stream);
    }
}
