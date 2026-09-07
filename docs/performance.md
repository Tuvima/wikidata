# Performance measurements

Phase 4 extracts bridge scoring, canonical rollups, and relationships into focused internal components. It preserves the public API and existing scoring/traversal algorithms. The benchmark harness establishes a comparison baseline for future optimization work.

## Run the offline harness

The standalone `benchmarks/Tuvima.Wikidata.Benchmarks` console project targets .NET 8 and .NET 10. It has no external package dependencies, does not call live providers, and is outside the solution's ordinary build/test/pack workflow.

From the repository root in PowerShell:

```powershell
New-Item -ItemType Directory -Path artifacts -Force | Out-Null
$env:DOTNET_TieredCompilation = '0'
dotnet run --project benchmarks/Tuvima.Wikidata.Benchmarks --configuration Release --framework net10.0 -- artifacts/benchmarks.json
```

Use `--framework net8.0` to measure the other runtime. Omit the final file argument to print results only. Run comparisons sequentially on the same otherwise-idle machine, using the same runtime, fixtures, and compilation settings. The environment setting affects this shell and child processes; remove it after benchmarking if desired.

Each workload performs 30 warm-up operations followed by seven samples of 100 operations. Reports include median/minimum/maximum elapsed milliseconds per operation, median allocated bytes per operation, and output checksums. Allocation measurements use process-wide `GC.GetTotalAllocatedBytes`; collections occur before each sample, outside the timer. Allocated bytes measure allocation traffic, not peak or retained memory.

The provider fixture includes JSON generation, HTTP response handling, deserialization, and the ordinary reconciliation pipeline. Caching and request pacing are disabled to make local CPU/allocation costs visible. A batch operation contains 20 inputs. Graph workloads use a 1,000-node binary tree: one path search from Q1 to Q1000 with a ten-hop limit, or construction of the entire graph.

## Phase 4 comparison

Measured locally on .NET 10.0.8, Windows build 26200, x64, with tiered compilation disabled. The baseline used the Phase 3 implementation before extraction, copied into an isolated build directory; the comparison used the refactored implementation. Both runs used identical fixtures and commands. [Machine-readable measurements](phase4-benchmarks.json) retain sample ranges, checksums, and comparison fingerprints.

| Workload | Before median ms | After median ms | Allocated bytes/op, before and after (rounded) |
|---|---:|---:|---:|
| Text reconciliation | 0.291 | 0.432 | 75,340 |
| Reconciliation batch, 20 inputs | 3.941 | 3.933 | 621,195 |
| Bridge ID with canonical rollup | 0.668 | 0.576 | 131,447 |
| Bridge batch, 20 inputs | 12.324 | 12.629 | 2,227,726 |
| Graph path, 1,000 nodes | 1.046 | 0.745 | 551,894 |
| Graph construction, 1,000 nodes | 0.560 | 0.419 | 432,606 |

Median allocation measurements were unchanged for every workload. Timings varied substantially even in untouched reconciliation and graph code, so these development-machine samples do not establish a speedup or a precise regression bound. No additional allocation or algorithm changes were made based on these timings. Future performance work should use repeated controlled runs and representative production datasets before setting thresholds.

Representative bridge-result fingerprints matched before/after, including all four rollup targets and text fallback. The fingerprint excludes timing/cache counters and includes candidates, scores, evidence, relationships, and semantic diagnostics. Exported public-member fingerprints also matched. These comparisons supplement the offline test suite; they do not replace comprehensive behavioral tests or binary-compatibility analysis.
