# BENCHMARK_PLAN.md - ToonNet Source Generator Performance Testing

**Created:** January 10, 2026  
**Status:** READY TO EXECUTE  
**Duration Estimate:** 60-90 minutes  

---

## Executive Summary

This plan outlines the comprehensive benchmarking strategy for ToonNet's Phase 3 source generator implementation. The goal is to quantify the performance gains of compile-time code generation versus reflection-based serialization.

**Expected Performance Gains:**
- Serialize: 3-5x faster
- Deserialize: 2-4x faster  
- Round-trip: 2.5-4.5x faster
- Memory: 40-60% less allocation

---

## Benchmark Infrastructure

### Project Structure
```
src/ToonNet.Benchmarks/
├── Program.cs              # BenchmarkDotNet runner
├── Models/
│   └── BenchmarkModels.cs  # 4 test models
└── ToonNet.Benchmarks.csproj
```

### Test Models (4 complexity levels)

**1. SimpleBenchmarkModel** (5 properties)
- string Name
- int Age
- string Email
- double Score
- bool IsActive

**2. MediumBenchmarkModel** (10 properties)
- string FirstName, LastName
- int Age
- string Email, Phone
- decimal Salary
- double Rating
- bool IsManager
- int DepartmentId
- long EmployeeId

**3. ComplexBenchmarkModel** (15 properties)
- 7 string properties (CompanyName, Address, City, State, ZipCode, Phone, Website)
- int FoundedYear
- decimal Revenue
- double EmployeeCount
- float MarketShare
- byte Status
- short SectorCode
- uint RegistrationId
- bool IsPublic

**4. NullableBenchmarkModel** (5 properties, 3 nullable)
- string? OptionalName
- int? OptionalAge
- decimal? OptionalSalary
- bool IsVerified
- string RequiredField

---

## Benchmark Scenarios

### Scenario 1: Simple Model Benchmarks
**Model:** SimpleBenchmarkModel (5 properties)

| Benchmark | Generated Code | Reflection | Expected Delta |
|-----------|----------------|------------|-----------------|
| Serialize | `SimpleBenchmarkModel.Serialize(obj)` | `ToonSerializer.Serialize(obj)` | 3-4x faster |
| Deserialize | `SimpleBenchmarkModel.Deserialize(doc)` | `ToonSerializer.Deserialize<T>(str)` | 2-3x faster |
| Round-trip | Serialize → Deserialize → Serialize | Same with reflection | 2.5-3.5x faster |

### Scenario 2: Medium Model Benchmarks
**Model:** MediumBenchmarkModel (10 properties)

| Benchmark | Generated Code | Reflection | Expected Delta |
|-----------|----------------|------------|-----------------|
| Serialize | `MediumBenchmarkModel.Serialize(obj)` | `ToonSerializer.Serialize(obj)` | 4-5x faster |
| Deserialize | `MediumBenchmarkModel.Deserialize(doc)` | `ToonSerializer.Deserialize<T>(str)` | 3-4x faster |
| Round-trip | Serialize → Deserialize → Serialize | Same with reflection | 3-4.5x faster |

### Scenario 3: Complex Model Benchmarks
**Model:** ComplexBenchmarkModel (15 properties)

| Benchmark | Generated Code | Reflection | Expected Delta |
|-----------|----------------|------------|-----------------|
| Serialize | `ComplexBenchmarkModel.Serialize(obj)` | `ToonSerializer.Serialize(obj)` | 4-5x faster |
| Deserialize | `ComplexBenchmarkModel.Deserialize(doc)` | `ToonSerializer.Deserialize<T>(str)` | 3-4x faster |
| Round-trip | Serialize → Deserialize → Serialize | Same with reflection | 3-4x faster |

### Scenario 4: Memory Allocation Comparison
**Focus:** GC allocation differences

| Operation | Generated Code | Reflection | Expected Reduction |
|-----------|----------------|------------|-------------------|
| Serialize small | <100 bytes | ~500-800 bytes | 50-80% less |
| Deserialize medium | <200 bytes | ~1-2 KB | 40-60% less |
| Round-trip complex | <500 bytes | ~2-3 KB | 50-70% less |

---

## BenchmarkDotNet Configuration

**Job Configuration:**
- Warmup: 3 iterations
- Target: 5 iterations
- Diagnostic: Memory allocation tracking
- Platform: net10.0 (Release build)

**Memory Diagnostics:**
- GC collections
- Bytes allocated per operation
- Peak working set

---

## Test Execution Plan

### Phase 1: Build Verification (5 min)
```bash
dotnet build src/ToonNet.Benchmarks/ToonNet.Benchmarks.csproj -c Release
```
✓ Verify generated code compiles  
✓ Check for warnings/errors  

### Phase 2: Run Benchmarks (60-75 min)
```bash
dotnet run -p src/ToonNet.Benchmarks/ToonNet.Benchmarks.csproj -c Release
```

**Execution Order:**
1. SimpleBenchmarksBatch (15-20 min)
   - 6 benchmarks × ~3-4 min per benchmark
   
2. MediumBenchmarksBatch (20-25 min)
   - 6 benchmarks × ~3-4 min per benchmark
   
3. ComplexBenchmarksBatch (20-25 min)
   - 6 benchmarks × ~3-4 min per benchmark

### Phase 3: Results Analysis (10-15 min)
- Parse output CSV from BenchmarkDotNet
- Extract performance deltas
- Create summary table
- Document findings

---

## Expected Output

BenchmarkDotNet will generate:
```
results/BenchmarkDotNet.Artifacts/
├── results/
│   ├── SimpleBenchmarksBatch-report.csv
│   ├── MediumBenchmarksBatch-report.csv
│   └── ComplexBenchmarksBatch-report.csv
└── logs/
```

**CSV Columns:**
- Method
- Toolchain
- Mean (ns)
- StdDev
- Median
- Allocated (bytes)
- Gen0, Gen1, Gen2 (GC collections)

---

## Success Criteria

### Performance Targets
✅ **Serialize:** Generated code 3-5x faster than reflection  
✅ **Deserialize:** Generated code 2-4x faster than reflection  
✅ **Round-trip:** Generated code 2.5-4.5x faster than reflection  
✅ **Memory:** Generated code 40-60% less allocation than reflection  

### Consistency Requirements
✅ Standard deviation < 10% of mean  
✅ All benchmarks complete without errors  
✅ Memory allocation predictable (no unexpected spikes)  

### Regression Prevention
✅ No performance degradation from Phase 2 tests  
✅ All 173 unit tests still passing  
✅ Build succeeds with 0 errors  

---

## Deliverables

After execution, the following will be created:

1. **BENCHMARK_RESULTS.md**
   - Summary tables with mean/std dev
   - Performance delta calculations
   - Memory allocation analysis
   - Per-model comparisons

2. **BenchmarkDotNet Reports**
   - CSV export for further analysis
   - HTML summary (if generated)
   - Detailed logs

3. **Performance Documentation**
   - Updated README with benchmarks
   - Performance section in DEVELOPMENT_STATUS.md
   - Optimization recommendations for Phase 4

---

## Next Steps After Execution

1. **Analyze Results**
   - Verify expectations met
   - Identify any anomalies
   - Document findings

2. **Optimization Opportunities**
   - If results are below target, identify bottlenecks
   - Plan Phase 4 improvements for collections/complex types

3. **Documentation**
   - Add benchmark results to project README
   - Document performance characteristics
   - Create migration guide for users

---

## Notes

- BenchmarkDotNet requires Release build for accurate results
- Each benchmark takes 3-4 minutes due to warmup and target iterations
- Results will vary based on system load - run when possible in isolation
- Ensure machine is not under heavy load during benchmarks
- Save CSV output for historical comparison

---

**READY TO PROCEED?** 
- ✅ Benchmark project created
- ✅ BenchmarkDotNet configured
- ✅ 4 test models defined
- ✅ 3 benchmark classes implemented
- ✅ Plan documented

**AWAITING USER CONFIRMATION TO RUN BENCHMARKS**

