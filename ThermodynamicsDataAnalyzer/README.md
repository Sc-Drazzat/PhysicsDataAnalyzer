# Thermodynamics Data Analyzer

## Purpose and research question

This .NET console application analyzes temperature measurements from constant-power heating experiments. It estimates a material's specific heat capacity, compares the estimate with a supplied reference value, and processes a folder of experiment JSON files in one run.

**Research question:** How closely does the apparent specific heat capacity calculated from heater energy and measured temperature change agree with the reference specific heat capacity?

The estimate is *apparent*: the current model treats all energy supplied by the heater as energy absorbed by the sample.

## Input format and units

Each input file is a JSON object with these fields:

| Field | Type | Unit / meaning |
|---|---|---|
| `experimentId` | string | Experiment identifier; required, nonblank |
| `material` | string | Material name; required, nonblank |
| `massKg` | number | Sample mass in kilograms; finite and greater than zero |
| `heaterPowerW` | number | Constant heater power in watts; finite and greater than zero |
| `specificHeatCapacityJPerKgK` | number | Reference specific heat capacity in J/(kg·K); finite and greater than zero |
| `measurements` | array | At least two measurement objects; entries cannot be null |
| `timeSeconds` | number | Measurement time in seconds; finite and strictly increasing |
| `temperatureCelsius` | number | Measured temperature in degrees Celsius; finite |

The dataset is [`Data/experiments`](Data/experiments). It contains five examples, including uneven heating, a two-measurement experiment, and an invalid zero-mass experiment. Property names are read case-insensitively.

The reader processes all `*.json` files in the selected folder in filename order. File or parsing errors return a structured `DomainError` with the operation and path. A dataset read builds records locally and returns them only when every file succeeds, so a malformed or unreadable file does not expose a partial dataset. A missing dataset folder is an input error. An existing empty folder is a valid dataset and produces an empty report.

## Physics, assumptions, and policies

The calculations use:

- Elapsed time: `Δt = t_final − t_initial` (s)
- Temperature change: `ΔT = T_final − T_initial` (°C difference, numerically equal to K difference)
- Supplied energy: `Q = P × Δt` (J), where `P` is in watts
- Whole-experiment apparent specific heat: `c = Q / (m × ΔT)` (J/(kg·K))
- Per-interval apparent specific heat: `cᵢ = Qᵢ / (m × ΔTᵢ)`, with `Qᵢ = P × Δtᵢ`
- Mean-interval estimate: arithmetic mean of all `cᵢ`
- Reported error: absolute difference `|c_est − c_reference|`; percentage `|c_est − c_reference| / c_reference × 100%`

The model assumes constant heater power and does not correct for heat loss, heating of the container or sensor, or changes in heater power. The whole-experiment policy uses the first and last measurements. The mean-interval policy calculates an estimate for each consecutive pair and averages them; it fails for an experiment containing an interval with zero or negative temperature change. Both policies are selectable at runtime.

## Domain validation

`ExperimentFactory.Create` returns `Result<ValidatedExperiment, DomainError>`. It rejects null input, blank experiment ID or material, nonfinite or nonpositive mass, heater power, or reference heat capacity, fewer than two measurements, null measurement entries, nonfinite measurement values, negative timestamps, and timestamps that are duplicate or decreasing. Each failure has a stable error code and a message that identifies the field, rule, experiment, or measurement index. The validated value has an internal constructor and stores a copied, read-only measurement snapshot. The production pipeline pattern-matches the `Success` and `Failure` cases. `ExperimentValidator.TryValidate` remains as a compatibility adapter and returns the structured error's message.

The factory evaluates the rules in order and returns the first failure. A success means every listed rule passed and includes a complete validated snapshot.

| Rule | Success condition | Failure code |
|---|---|---|
| Input | Input object is present | `experiment.input.required` |
| Experiment ID | ID is nonblank | `experiment.id.required` |
| Material | Material is nonblank | `experiment.material.required` |
| Mass | Finite and greater than zero | `experiment.mass.positive_finite` |
| Heater power | Finite and greater than zero | `experiment.heater_power.positive_finite` |
| Reference heat capacity | Finite and greater than zero | `experiment.reference_heat_capacity.positive_finite` |
| Measurements | At least two entries are present | `experiment.measurements.minimum` |
| Measurement entry | Every entry is nonnull | `experiment.measurement.required` |
| Measurement values | Time and temperature are finite | `experiment.measurement.finite` |
| Measurement time | Time is nonnegative | `experiment.measurement.time.nonnegative` |
| Measurement order | Each time is greater than the preceding time | `experiment.measurement.time.increasing` |

Zero is a valid measured temperature, but zero is not valid for mass, power, or the reference heat capacity. Negative time is rejected; temperatures may be negative when finite. The estimate stage separately fails when the selected policy requires a positive temperature change and cannot produce a positive finite estimate.

## Lookup and Result composition

`ExperimentCatalog.FindById` returns `Maybe.Some(record)` whenever an ID exists in the catalog, even if the record will later fail validation or contains a legitimate zero measurement. An absent or blank ID returns `Maybe.None`. In `ExperimentPipeline.Analyze`, the private `Lookup` function converts `Some` to a successful `Result` and `None` to `DomainError.Codes.ExperimentNotFound` with the requested ID in its message.

The production chain in `Pipeline/ExperimentPipeline.cs` is:

```csharp
Lookup(catalog, experimentId)
    .Bind(ExperimentFactory.Create)
    .Bind(experiment => Calculate(experiment, estimator))
    .Map(data => CreateExperimentReport(data.Experiment, policy, data.Estimate));
```

Each `Bind` starts the next operation only after a success. A lookup or validation failure propagates without calling calculation. `Map` creates the report only after calculation succeeds. `ResultTests.BindPreservesFailureWithoutCallingFunction` contains the direct failure-propagation assertion; pipeline tests additionally verify calculation is skipped after lookup and validation failures.

Both estimator policies reject a nonpositive temperature change when it is needed for an estimate, and they reject nonfinite calculated estimates. As a result, whole-experiment analysis requires a positive net temperature change, while mean-interval analysis requires every interval to have a positive temperature change. Per-record validation or calculation failures are included in the batch report's `Rejected` list. Dataset-folder and report-writing errors are reported by the console application and return a nonzero exit status.

`ExperimentPipeline.Analyze` composes catalog lookup, validation, policy calculation, and report creation with `Result.Bind` and `Result.Map`. It returns `Result<ExperimentReport, DomainError>`. Missing experiments, invalid input, unsupported policies, and rejected estimates are structured failures. A failure stops the chain before later steps run. Batch processing matches the final success or failure and adds the record to the accepted or rejected list, preserving the existing batch report behavior. The pipeline accepts one policy enum and selects the matching estimator itself, so callers cannot pass a `WholeExperiment` label alongside the mean estimator.

Analysis is repeatable and does not mutate its input: analyzing the same experiment with the same policy again returns the same report. Validation copies measurements into a read-only snapshot, and batch processing builds accepted and rejected entries in local lists before creating the final report. A rejected record does not change the input experiment or prevent the rest of the batch from being processed.

## Unsafe before: hidden calculation prerequisites

> Documentation-only example. This unsafe call site is not used by the active application.

A specific-heat calculation depends on having valid measurement data and choosing which estimation policy to use. A less safe workflow could calculate directly from raw input before checking either prerequisite:

```csharp
ExperimentInput input = ReadRawExperiment();
double estimate = CalculateSpecificHeat(input);
```

The prerequisites are valid measurements, positive mass, heater power and reference heat capacity, and a selected calculation policy (`whole` or `mean`). The required order is to validate the raw experiment and select the policy first, then calculate using the resulting `ValidatedExperiment`. If validation or policy selection is skipped or happens after calculation, the program could divide by invalid values, produce an incorrect estimate, or create a report that cannot be trusted. The active workflow makes these steps explicit through `ExperimentFactory.Create`, policy selection in `Program.cs`, and the analysis pipeline.

## Pipeline and output

The workflow reads command-line arguments, selects `whole` or `mean`, then calls `ExperimentFileReader.TryReadDataset`. `ExperimentPipeline.AnalyzeWithReports` calls the existing `AnalyzeBatch`, preserving batch `Result` failures and accepted/rejected per-record outcomes. It takes only accepted `ExperimentReport` values and passes them to `ExperimentStream.EnumerateEnergyConversions` with `NativeEnergyAdapter`.

The conversion sequence is materialized once with `ToArray()`. If any conversion returns `Result.Failure`, the workflow returns that same `DomainError`; accuracy, material summaries, and score views are built only after every accepted report converts successfully. Successful conversion values are unwrapped into `EnergyConversionReport` rows with a numeric `WattHours` field before JSON serialization. The console writes and prints the combined report only after the full workflow succeeds. Input, analysis/native conversion, and output errors print to stderr and return a nonzero exit code.

The command-line arguments are `DATASET_DIRECTORY [OUTPUT_JSON] [whole|mean]`. If omitted, the app locates `Data/experiments`, selects `whole`, and writes a policy-named JSON file in `Data/reports`. The output directory is created when needed. Per-record validation and calculation failures remain in `BatchReport.Rejected`, and the console prints each rejection after writing the complete report.

For the included five-file dataset, whole-experiment analysis accepts four experiments and rejects `experiment-004.json` because its mass is zero. The accuracy ranking orders them by percentage error descending: oil (`70%`), aluminum (`48.1481%`), water experiment 001 (`8.7429%`), and water experiment 002 (`4.3062%`). Material summaries include water (2 experiments, `18,000 J` total), aluminum (1, `3,600 J`), and oil (1, `1,500 J`). Water's mean estimate is approximately `4,272.73 J/(kg·K)` with mean percentage error `6.5246%`. For experiment 001, whole-experiment and mean-interval estimates are approximately `4,545.45` and `5,333.33 J/(kg·K)` respectively, against a reference of `4,180 J/(kg·K)`. The batch example remains at [`Data/reports/batch-report-whole.json`](Data/reports/batch-report-whole.json).

## Reports and scores

`BuildAccuracyRanking` reports one row per reportable experiment. It uses `WhereReportable` to require a finite estimate and a finite, positive reference heat capacity, then `ToAccuracyRows` to project the experiment ID, material, policy, estimate, reference, absolute error, and percentage error. Rows sort by percentage error descending and experiment ID ascending using ordinal comparison.

`BuildMaterialSummaries` groups reportable values by material with ordinal grouping. It reports material, experiment count, mean estimated heat capacity, total supplied energy in joules, and mean percentage error. Inputs with nonfinite percentage error or energy are excluded from this aggregation. Rows sort by total energy descending, then material ascending using ordinal comparison.

The sample accuracy values and material aggregates are shown above. For the first water experiment, the score configurations produce different results from the same `8.7429%` error: lenient penalty `0.5` gives about `95.6285`, while strict penalty `2.0` gives about `82.5141`. `ExperimentScore.CreateScore` is curried: it captures a `ScoreConfiguration` and returns a `Func<ExperimentReport, double>` using `Math.Max(0, 100 - percentageError × PenaltyPerErrorPercent)`. The scores are clamped at zero.

`WhereReportable` and `ToAccuracyRows` extend `IEnumerable<ExperimentReport>` because both are reusable sequence operations that can compose with arrays, lists, and other LINQ sequences. Their receiver type avoids tying the transformations to one collection implementation.

## Deferred conversions, snapshots, and ownership

`ExperimentStream.EnumerateEnergyConversions` is lazy: creating its enumerable performs no conversion. Each enumeration calls the converter again for every report. `AnalyzeWithReports` calls `ToArray()` once and reuses that snapshot while checking failures and building the output. Reusing the array does not repeat native calls.

The input reader owns and closes each file read before returning records. Validated experiments own copied, read-only measurement snapshots. The conversion iterator owns no files or native handles; the runtime owns the loaded native library for the process lifetime. The workflow retains materialized reports and conversions in `ExperimentAnalysisReports`, so memory use grows with the number of accepted experiments. Streaming avoids retaining all conversions but repeats work on another pass; a snapshot costs memory and gives stable, repeatable access without recalculation. There are no explicit cleanup calls for the native library handle.

## Native energy conversion

The C function `pda_joules_to_watt_hours(double)` returns `-1.0` for negative input and `joules / 3600.0` otherwise. `NativeEnergyAdapter` rejects negative, NaN, and infinite inputs before calling C, and turns library or missing-entry-point errors into `Result<double, DomainError>` failures. The P/Invoke declaration uses `double` for both the parameter and result, the exact C export name, and the C calling convention.

The verified build target is macOS arm64. It was compiled with Apple clang 21.0.0 (`clang-2100.0.123.102`) using this command from the repository root:

On Windows, run `ThermodynamicsDataAnalyzer/native/energy/build-windows.bat` from a Visual Studio Developer Command Prompt. It uses MSVC `cl.exe` to create `native/energy/build/windows-x64/pda_energy.dll`, which the project copies beside the application when building.

```bash
mkdir -p ThermodynamicsDataAnalyzer/native/energy/build/macos-arm64
clang -arch arm64 -dynamiclib -std=c11 -O2 -Wall -Wextra -Werror -o ThermodynamicsDataAnalyzer/native/energy/build/macos-arm64/libpda_energy.dylib ThermodynamicsDataAnalyzer/native/energy/energy_conversion.c
```

The output is `ThermodynamicsDataAnalyzer/native/energy/build/macos-arm64/libpda_energy.dylib`. On Windows x64, compile the same source from a Visual Studio Developer Command Prompt with `cl /LD /O2 /W4 ThermodynamicsDataAnalyzer\native\energy\energy_conversion.c /Fe:ThermodynamicsDataAnalyzer\native\energy\build\windows-x64\pda_energy.dll`; the source exports the function with `__declspec(dllexport)`. A Windows compiler was not available for verification in this environment, and no Windows DLL is checked in.

The project files copy an existing macOS dylib or Windows DLL beside the application and test assemblies. The runtime loads `pda_energy` by the operating system's native naming rules. The previous integration issue was that a library in the native source tree was not beside the managed assembly; copying the compiled artifact to the output directory resolves lookup for normal app and test runs. A manual native call with `3600` joules returned `1` watt-hour; the adapter test also checks this real conversion.

## Build and run

The projects target `net10.0`. From the repository root:

```bash
dotnet restore PhysicsDataAnalyzer.sln
dotnet build PhysicsDataAnalyzer.sln
dotnet run --project ThermodynamicsDataAnalyzer/ThermodynamicsDataAnalyzer.csproj
```

Run the whole-experiment policy using the default report path:

```bash
dotnet run --project ThermodynamicsDataAnalyzer/ThermodynamicsDataAnalyzer.csproj -- ThermodynamicsDataAnalyzer/Data/experiments
```

Choose an explicit output path and the mean-interval policy:

```bash
dotnet run --project ThermodynamicsDataAnalyzer/ThermodynamicsDataAnalyzer.csproj -- ThermodynamicsDataAnalyzer/Data/experiments ThermodynamicsDataAnalyzer/Data/reports/my-mean-report.json mean
```

To demonstrate an input error caused by a missing dataset folder:

```bash
dotnet run --project ThermodynamicsDataAnalyzer/ThermodynamicsDataAnalyzer.csproj -- /tmp/does-not-exist
```

This prints an `Input error` and exits with a nonzero status. To demonstrate a rejected experiment while the batch continues, run against the included dataset: the zero-mass file appears in the report's `Rejected` list and the valid experiments are still analyzed. The integrated workflow returns a failure and writes no output report when native conversion fails.

For the remaining failure demonstrations, run the focused tests from the repository root. They create and remove temporary files and directories themselves:

```bash
dotnet test PhysicsDataAnalyzer.sln --no-restore --filter 'FullyQualifiedName~ExperimentCatalogTests.FindsExperimentById'
dotnet test PhysicsDataAnalyzer.sln --no-restore --filter 'FullyQualifiedName~ExperimentPipelineTests.ReturnsNotFoundFailureForMissingExperiment'
dotnet test PhysicsDataAnalyzer.sln --no-restore --filter 'FullyQualifiedName~ExperimentFactoryTests.RejectsEveryValidationRuleWithAUsefulStructuredError'
dotnet test PhysicsDataAnalyzer.sln --no-restore --filter 'FullyQualifiedName~ExperimentPipelineTests.ReturnsDomainFailureWhenCalculationCannotProducePositiveEstimate'
dotnet test PhysicsDataAnalyzer.sln --no-restore --filter 'FullyQualifiedName~ExperimentFileReaderTests.ReportsMissingFileWithOperationAndPath'
dotnet test PhysicsDataAnalyzer.sln --no-restore --filter 'FullyQualifiedName~ExperimentFileReaderTests.ReportsMalformedJsonWithOperationAndPath'
dotnet test PhysicsDataAnalyzer.sln --no-restore --filter 'FullyQualifiedName~ExperimentWorkflowTests.ReturnsNativeFailureWithoutProducingPartialReports'
dotnet test PhysicsDataAnalyzer.sln --no-restore --filter 'FullyQualifiedName~NativeEnergyAdapterTests.ConvertsLoadingFailureToStructuredError'
```

The factory test includes mass, minimum measurement count, and timestamp-order failures among all factory rules. The final two file-reader tests verify the error operation and temporary path. The malformed-dataset test also confirms no partial record list is returned when a later file fails.

## Automated tests and latest result

The solution targets `net10.0` and was verified with .NET SDK `10.0.400`. Run the exact commands below from the repository root (`PhysicsDataAnalyzer/`):

```bash
dotnet --version
dotnet restore PhysicsDataAnalyzer.sln
dotnet build PhysicsDataAnalyzer.sln --no-restore
dotnet run --project ThermodynamicsDataAnalyzer/ThermodynamicsDataAnalyzer.csproj
dotnet test PhysicsDataAnalyzer.sln --no-restore
```

The suite has 77 passing test cases, including more than 8 pure-function and domain cases, two complete report workflow tests, and missing-file and malformed-JSON boundary cases. Coverage includes report query values and tie ordering, extension filtering and projection, scoring, deferred conversion and snapshots, real native conversion, adapter input and loading failures, result propagation, and accepted/rejected batch processing. File-reader boundary tests use temporary paths and remove their directories in `finally` blocks.

Verified with .NET SDK `10.0.400` on 2026-10-31 using `dotnet test ThermodynamicsDataAnalyzer.Tests/ThermodynamicsDataAnalyzer.Tests.csproj --no-restore -m:1 --verbosity quiet`: 77 passed, 0 failed, 0 skipped. The test project build completed with 0 warnings and 0 errors.

## Requirement-to-file and method mapping

| Requirement | Implemented location |
|---|---|
| Console boundary handling, CLI policy selection, errors, and output | `ThermodynamicsDataAnalyzer/Program.cs` — dataset and batch `Result` matching, `FindDefaultDatasetPath` |
| JSON experiment and dataset reading with structured errors | `IO/ExperimentFileReader.cs` — `TryReadExperiment`, `TryReadDataset`; `Models/Result.cs`; `Models/DomainError.cs` |
| Input data models | `Models/ExperimentInput.cs`, `Models/TemperatureMeasurement.cs`, `Models/ExperimentFileRecord.cs` |
| Structured domain validation and isolated validated snapshot | `Validation/ExperimentFactory.cs` — `Create`; `Models/Result.cs`; `Models/DomainError.cs`; `Models/ValidatedExperiment.cs` |
| Time, temperature, and energy calculations | `Physics/ThermodynamicsCalculator.cs` — `CalculateElapsedTimeSeconds`, `CalculateTemperatureChangeCelsius`, `CalculateEnergySupplied`, `CalculateIntervalEnergySupplied` |
| Interval construction | `Physics/IntervalCalculator.cs` — `CalculateIntervals`; `Models/CalculatedInterval.cs` |
| Specific heat estimators | `Physics/HeatCapacityEstimator.cs` — `TryEstimateWholeExperiment`, `TryEstimateMeanInterval` |
| Policy selection and matching estimator | `Pipeline/ExperimentPipeline.cs` — `SelectEstimator`; calculation functions in `Pipeline/AnalysisPolicy.cs` |
| Explicit experiment lookup | `Models/Maybe.cs`; `Pipeline/ExperimentCatalog.cs` — `FindById` |
| Result composition with `Bind` and `Map`, and accepted/rejected batch outcomes | `Models/Result.cs`; `Models/DomainError.cs`; `Pipeline/ExperimentPipeline.cs` — `Lookup`, `Analyze`, `AnalyzeBatch`, `TryAnalyzeBatch`, `CreateResult`; `Models/ExperimentBatchReport.cs` |
| Report models and JSON output | `Models/ExperimentReport.cs`, `Models/AnalysisResult.cs`, `IO/ReportWriter.cs` — `TryWriteReport`, `TryWriteBatchReport` |
| Accuracy and material reports | `Models/ExperimentAccuracyRow.cs`, `Models/MaterialSummaryRow.cs`, `Reports/ExperimentReportQueries.cs` — `BuildAccuracyRanking`, `BuildMaterialSummaries` |
| Reusable report sequences | `Extensions/ExperimentReportExtensions.cs` — `WhereReportable`, `ToAccuracyRows` |
| Curried scoring | `Pipeline/ExperimentScore.cs` — `ScoreConfiguration.Lenient`, `ScoreConfiguration.Strict`, `CreateScore`; `Reports/ExperimentReportQueries.cs` — `BuildScoredReports` |
| Deferred native conversion | `Pipeline/ExperimentStream.cs` — `INativeEnergyConverter`, `EnumerateEnergyConversions`; `Interop/NativeEnergy.cs`; `Interop/NativeEnergyAdapter.cs`; `native/energy/energy_conversion.c` |
| Combined report workflow | `Pipeline/ExperimentPipeline.cs` — `AnalyzeWithReports`; `Models/ExperimentAnalysisReports.cs`; `Program.cs`; `IO/ReportWriter.cs` — `TryWriteJson` |
| Report and native tests | `ExperimentWorkflowTests.cs`, `ExperimentReportQueriesTests.cs`, `ExperimentReportExtensionsTests.cs`, `ExperimentScoreTests.cs`, `ExperimentStreamTests.cs`, `NativeEnergyAdapterTests.cs`, `ExperimentFileReaderTests.cs` |
| Result, factory, lookup, reader, pipeline, and physics tests | `ThermodynamicsDataAnalyzer.Tests/ResultTests.cs`, `ExperimentFactoryTests.cs`, `ExperimentCatalogTests.cs`, `ExperimentFileReaderTests.cs`, `ExperimentPipelineTests.cs`, `ExperimentBatchPipelineTests.cs`, `ExperimentValidatorTests.cs`, `ThermodynamicsCalculatorTests.cs`, `SpecificHeatEstimatorTests.cs`, `TestData.cs` |

## Report sequences and snapshots

`WhereReportable` and `ToAccuracyRows` are extension methods on `IEnumerable<ExperimentReport>` because filtering and projection are reusable sequence operations. This lets callers compose them with different report sources and other LINQ operations without requiring a particular collection type.

`ExperimentStream.EnumerateEnergyConversions` is deferred: conversion occurs during enumeration. Enumerating its original sequence again repeats conversion. When multiple passes need consistent converted values, materialize once with `ToArray()` and reuse that array; reusing the array does not repeat conversion.

## Submission archive

The submission archive contains the solution, application and test source, README, and sample input and report JSON. Generated `bin` and `obj` folders, IDE caches, and user-specific IDE settings are excluded. Restore packages and rebuild after extracting the archive.

## Author and contributions

| Name | Group | Concrete contribution |
|---|---|---|
| Ayan Rakhimov | IT-2513 | Implemented the experiment models and validation, thermodynamics calculations, both specific-heat policies, JSON dataset reader and report writer, batch and combined report pipelines, report queries and scoring, native conversion integration, example experiment data, tests, and this documentation. |
