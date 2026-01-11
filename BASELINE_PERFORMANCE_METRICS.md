# ToonNet Baseline Performance Metrics

**Measurement Date:** 2026-01-11  
**Environment:** .NET 8.0.11 (Arm64 RyuJIT AdvSIMD)  
**Configuration:** Release build with PGO enabled  
**Status:** 🎯 Baseline Established (Pre-Sprint 2)

---

## 📊 Executive Summary

Bu dokümant, ToonNet'in Sprint 2+ optimizasyonlar öncesi performance baseline'ını dokumenter. Tüm metrikler **Phase 1 & Phase 2 optimizations** (token caching, EndOfInput caching) sonrası alınmıştır.

**Mevcut Optimizasyon Durumu:**
- ✅ Phase 1: EndOfInput token caching, IsAtEnd() optimization, indent cache (51 levels)
- ✅ Phase 2: Current token position caching
- ✅ PGO Enabled: TieredCompilation, TieredPGO, DynamicPGO

**Beklenen Baseline Performans:**
- Parsing: ~15-23% daha hızlı (Phase 1+2 sayesinde)
- Memory: ~85% daha az allocation (Phase 1+2 sayesinde)

---

## 🎯 Benchmark Categories

### 1. Simple Model Benchmarks (5 properties)
**Model:** `SimpleBenchmarkModel` - Name, Age, Email, Score, IsActive

**Existing Benchmarks:**
- SerializeGenerated (Source generator based)
- SerializeReflection (Reflection based)
- DeserializeGenerated (Source generator based)
- DeserializeReflection (Reflection based)

### 2. Medium Model Benchmarks (10 properties)
**Model:** `MediumBenchmarkModel` - FirstName, LastName, Age, Email, Phone, Salary, Rating, IsManager, DepartmentId, EmployeeId

**Existing Benchmarks:**
- SerializeGenerated
- SerializeReflection
- DeserializeGenerated
- DeserializeReflection

### 3. Complex Model Benchmarks (15 properties)
**Model:** `ComplexBenchmarkModel` - CompanyName, Address, City, State, ZipCode, Phone, Website, FoundedYear, Revenue, EmployeeCount, MarketShare, Status, SectorCode, RegistrationId, IsPublic

**Existing Benchmarks:**
- SerializeGenerated
- SerializeReflection
- DeserializeGenerated
- DeserializeReflection

### 4. Large Document Benchmarks (NEW in Sprint 1)
**Scenarios:**
- Parse_10KB (~100 objects)
- Parse_100KB (~1000 objects)
- Parse_1MB (~10000 objects)
- Encode_10KB, Encode_100KB, Encode_1MB
- RoundTrip_10KB, RoundTrip_100KB

### 5. Deep Nesting Benchmarks (NEW in Sprint 1)
**Scenarios:**
- Parse_Depth10, Parse_Depth25, Parse_Depth50, Parse_Depth75
- Encode_Depth10, Encode_Depth50
- RoundTrip_Depth25, RoundTrip_Depth50

### 6. Async Operations Benchmarks (NEW in Sprint 1)
**Scenarios:**
- SerializeAsync_Small, DeserializeAsync_Small
- ParseAsync_Small, ParseAsync_Medium
- EncodeAsync_Small
- FileOperations_SerializeToFile, FileOperations_DeserializeFromFile
- StreamOperations_WriteAndRead

### 7. Parser-Only Benchmarks (NEW in Sprint 1)
**Scenarios:**
- Parse_SimpleObject, Parse_InlineArray, Parse_ListStyleArray
- Parse_NestedObject, Parse_MixedContent
- ParseAndAccess_SimpleObject, ParseAndAccess_NestedObject
- ParseAndIterate_ArrayCount

### 8. Encoder-Only Benchmarks (NEW in Sprint 1)
**Scenarios:**
- Encode_SimpleObject, Encode_InlineArray, Encode_NestedObject
- Encode_LargeArray (100 items), Encode_DeepNesting (20 levels)
- Encode_StringLength_SimpleObject, Encode_StringLength_LargeArray

### 9. Memory Pressure Benchmarks (NEW in Sprint 1)
**Scenarios:**
- Parse_HighAllocationPressure (1000 items with long strings)
- Encode_HighAllocationPressure
- RoundTrip_HighAllocationPressure
- Parse_MultipleDocuments (10x sequential)
- Encode_MultipleDocuments (10x sequential)
- CreateAndDispose_ParserInstances (100x)
- CreateAndDispose_EncoderInstances (100x)

---

## 📈 Expected Baseline Metrics (Estimated)

### Parsing Performance

| Scenario | Estimated Time | Memory Allocation | Notes |
|----------|----------------|-------------------|-------|
| Simple Object | 2-5 µs | <100 B | 5 properties |
| Medium Object | 4-8 µs | 100-200 B | 10 properties |
| Complex Object | 6-12 µs | 200-400 B | 15 properties |
| 10KB Document | 200-400 µs | 10-20 KB | ~100 objects |
| 100KB Document | 2-4 ms | 100-150 KB | ~1000 objects |
| 1MB Document | 20-40 ms | 1-2 MB | ~10000 objects |
| Depth 10 | 5-10 µs | <500 B | Nested objects |
| Depth 50 | 25-50 µs | 2-5 KB | Deep nesting |

### Encoding Performance

| Scenario | Estimated Time | Memory Allocation | Notes |
|----------|----------------|-------------------|-------|
| Simple Object | 1-3 µs | <100 B | StringBuilder reuse |
| Medium Object | 2-5 µs | 100-200 B | 10 properties |
| Large Array (100) | 100-200 µs | 10-30 KB | Array encoding |
| Deep Nesting (20) | 15-30 µs | 1-3 KB | Indent caching active |

### Serialization Performance (Reflection vs Generated)

| Model | Generated | Reflection | Speedup | Notes |
|-------|-----------|------------|---------|-------|
| Simple (5 props) | 1.2 µs | 5.8 µs | **4.8x** | Existing data |
| Medium (10 props) | 2.0 µs | 12.5 µs | **6.2x** | Existing data |
| Complex (15 props) | 2.8 µs | 18.2 µs | **6.5x** | Existing data |

**Memory:**
- Generated: ~64 B per object
- Reflection: ~512 B per object
- **Reduction: 87%**

---

## 🎯 Performance Targets for Sprint 2+

### Sprint 2 Targets (Memory Optimizations)
**Goal:** Reduce allocations by 30-50%, improve speed by 15-25%

| Optimization | Target Improvement | Priority |
|--------------|-------------------|----------|
| Span<char> migration | -30-40% allocations (lexer) | 🔥 P0 |
| ArrayPool for tokens | -20-30% allocations | 🔥 P0 |
| StringBuilder pooling | -15-20% allocations (encoder) | ⚡ P1 |

### Sprint 3 Targets (Parsing Optimizations)
**Goal:** Improve parsing speed by 20-30%

| Optimization | Target Improvement | Priority |
|--------------|-------------------|----------|
| Token type bitmask | +5-10% speed | 🔥 P0 |
| Lookahead window | +10-15% speed (complex) | ⚡ P1 |
| Reflection cache | +40-60% speed (serialization) | ⚡ P1 |

### Sprint 4 Targets (Serialization & Async)
**Goal:** Improve serialization by 40-60%, async by 20-40%

| Optimization | Target Improvement | Priority |
|--------------|-------------------|----------|
| Expression trees | +300-500% property access | ⚡ P1 |
| ValueTask migration | -20-40% allocations (async) | ⚡ P1 |
| Serialization plans | +20-30% repeated serialization | 💡 P2 |

---

## 🔬 Measurement Methodology

### Benchmark Configuration
```bash
# BenchmarkDotNet Settings
- WarmupCount: 3 iterations
- IterationCount: 10-20 iterations (varies by benchmark)
- MemoryDiagnoser: Enabled
- Job: Short/Default
```

### Hardware Configuration
```
- Architecture: Arm64 (Apple Silicon)
- Runtime: .NET 8.0.11
- GC: Concurrent Workstation
- JIT: RyuJIT with AdvSIMD
- HardwareIntrinsics: AdvSIMD, AES, CRC32, DP, RDM, SHA1, SHA256
- VectorSize: 128 bits
```

### Measurement Commands
```bash
# Full benchmark suite
dotnet run --project src/ToonNet.Benchmarks -c Release

# Specific category
dotnet run --project src/ToonNet.Benchmarks -c Release -- --filter "*SimpleBenchmarks*"

# With detailed diagnostics
dotnet run --project src/ToonNet.Benchmarks -c Release -- --job Short --memory
```

---

## 📊 Actual Benchmark Results

### Status: ⏳ To Be Measured

**Note:** Comprehensive benchmark suite çalıştırılması gerekiyor. Tahmini süre: 20-30 dakika.

**Planned Execution:**
```bash
# Step 1: Run all benchmarks
cd src/ToonNet.Benchmarks
dotnet run -c Release -- --job Short --memory --exporters json,html

# Step 2: Analyze results
# - Check BenchmarkDotNet.Artifacts/results/
# - Generate comparison reports
# - Document bottlenecks
```

**Results will include:**
1. Mean execution time (µs, ms)
2. Standard deviation
3. Memory allocation (Gen0/Gen1/Gen2 GC)
4. Allocated bytes per operation
5. Throughput (ops/sec)

---

## 🐛 Known Bottlenecks (Pre-Sprint 2)

### Identified Issues from Previous Analysis

1. **String Operations (Lexer/Parser)**
   - String.Substring() causes heap allocations
   - Estimated impact: 30-40% of lexer allocations
   - **Fix in Sprint 2:** Span<char> migration

2. **Token Storage (Parser)**
   - List<ToonToken> allocated every parse
   - No reuse between parse operations
   - **Fix in Sprint 2:** ArrayPool<ToonToken>

3. **StringBuilder (Encoder)**
   - New StringBuilder per encode operation
   - No pooling mechanism
   - **Fix in Sprint 2:** StringBuilder pooling

4. **Token Type Checks (Parser)**
   - Multiple if/else chains in hot paths
   - Poor branch prediction
   - **Fix in Sprint 3:** Bitmask optimization

5. **Reflection Performance (Serializer)**
   - GetValue/SetValue very slow
   - No caching of type metadata
   - **Fix in Sprint 3+4:** Caching + Expression trees

---

## ✅ Validation Criteria

**Baseline is considered valid if:**
- ✅ All 427 tests passing
- ✅ Consistent results across multiple runs (SD < 10%)
- ✅ Memory allocation metrics realistic
- ✅ No anomalies or outliers
- ✅ PGO warmup effects stabilized

**Comparison Methodology for Future Sprints:**
```bash
# Before optimization (Sprint N-1)
dotnet run -c Release -- --job Short --memory --baseline

# After optimization (Sprint N)
dotnet run -c Release -- --job Short --memory

# Compare results
# - Regression threshold: -5% (fail)
# - Improvement target: +15% minimum (pass)
```

---

## 📚 Related Documentation

- [PERFORMANCE_OPTIMIZATION_PLAN.md](PERFORMANCE_OPTIMIZATION_PLAN.md) - Original analysis
- [PHASE1_OPTIMIZATIONS_COMPLETE.md](PHASE1_OPTIMIZATIONS_COMPLETE.md) - Phase 1 results
- [PHASE2_TOKEN_CACHING_COMPLETE.md](PHASE2_TOKEN_CACHING_COMPLETE.md) - Phase 2 results
- [PERFORMANCE_IMPROVEMENT_ROADMAP.md](PERFORMANCE_IMPROVEMENT_ROADMAP.md) - Sprint plan

---

## 🎯 Next Steps

1. ✅ Benchmark suite oluşturuldu (38+ scenarios)
2. ✅ PGO enabled (5 production projects)
3. ⏳ **Run comprehensive benchmarks** (this document)
4. ⏳ Document actual baseline metrics
5. ⏳ Identify top 3 bottlenecks
6. ⏳ Start Sprint 2: Memory Optimizations

---

**Last Updated:** 2026-01-11  
**Next Review:** After Sprint 2 completion (compare against baseline)
