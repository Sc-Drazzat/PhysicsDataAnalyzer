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
double estimate = CalculateSpecificHeat(input); // Was it validated? Which policy is being used?
```

The prerequisites are valid measurements, positive mass, heater power and reference heat capacity, and a selected calculation policy (`whole` or `mean`). The required order is to validate the raw experiment and select the policy first, then calculate using the resulting `ValidatedExperiment`. If validation or policy selection is skipped or happens after calculation, the program could divide by invalid values, produce an incorrect estimate, or create a report that cannot be trusted. The active workflow makes these steps explicit through `ExperimentFactory.Create`, policy selection in `Program.cs`, and the analysis pipeline.

## Pipeline and output

The workflow is:

1. `Program.cs` reads optional command-line arguments and selects the whole-experiment or mean-interval policy enum.
2. `IO/ExperimentFileReader.cs` reads JSON files into a local list and returns the dataset only when every file succeeds. A read or parse failure includes the operation and path and stops the console workflow before analysis.
3. `Validation/ExperimentFactory.cs` validates each successfully read experiment and returns `Result<ValidatedExperiment, DomainError>`; success carries an isolated snapshot and failure carries a structured domain error.
4. `Physics/ThermodynamicsCalculator.cs` calculates elapsed time, temperature change, and supplied energy. `Physics/IntervalCalculator.cs` creates interval values for the mean-interval policy.
5. `Pipeline/ExperimentPipeline.cs` selects the estimator for the chosen policy; `Pipeline/AnalysisPolicy.cs` calls the relevant calculation and estimator stages in `Physics/HeatCapacityEstimator.cs`.
6. `Pipeline/ExperimentPipeline.cs` returns a structured `Result` for each experiment and for batch-level failures. Per-experiment validation or calculation failures become rejected entries; `Program.cs` matches the batch result and prints each rejection reason.
7. The successful batch report is printed to stdout and written as indented JSON by `IO/ReportWriter.cs`. Enum values are serialized as strings.

The command-line arguments are `DATASET_DIRECTORY [OUTPUT_JSON] [whole|mean]`. If omitted, the application locates `Data/experiments`, selects `whole`, and writes a policy-named report in `Data/reports`. The output directory is created when needed. The console matches reader and batch-analysis `Result` cases explicitly. It prints file or dataset errors before stopping; after successful batch processing it prints each rejected file and its reason, then writes the complete report.

For the included five-file dataset, the whole-experiment report contains four accepted experiments and one rejected experiment (`experiment-004.json`, whose mass is zero). For `experiment-001.json`, the whole-experiment estimate is approximately 4545.45 J/(kg·K), while the mean-interval estimate is approximately 5333.33 J/(kg·K). Its reference value is 4180 J/(kg·K). The checked-in whole-policy example is [`Data/reports/batch-report-whole.json`](Data/reports/batch-report-whole.json).

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

This prints an `Input error` and exits with a nonzero status. To demonstrate a rejected experiment while the batch continues, run against the included dataset: the zero-mass file appears in the report's `Rejected` list and the valid experiments are still analyzed.

For the remaining failure demonstrations, run the focused tests from the repository root. They create and remove temporary files and directories themselves:

```bash
dotnet test PhysicsDataAnalyzer.sln --no-restore --filter 'FullyQualifiedName~ExperimentCatalogTests.FindsExperimentById'
dotnet test PhysicsDataAnalyzer.sln --no-restore --filter 'FullyQualifiedName~ExperimentPipelineTests.ReturnsNotFoundFailureForMissingExperiment'
dotnet test PhysicsDataAnalyzer.sln --no-restore --filter 'FullyQualifiedName~ExperimentFactoryTests.RejectsEveryValidationRuleWithAUsefulStructuredError'
dotnet test PhysicsDataAnalyzer.sln --no-restore --filter 'FullyQualifiedName~ExperimentPipelineTests.ReturnsDomainFailureWhenCalculationCannotProducePositiveEstimate'
dotnet test PhysicsDataAnalyzer.sln --no-restore --filter 'FullyQualifiedName~ExperimentFileReaderTests.ReportsMissingFileWithOperationAndPath'
dotnet test PhysicsDataAnalyzer.sln --no-restore --filter 'FullyQualifiedName~ExperimentFileReaderTests.ReportsMalformedJsonWithOperationAndPath'
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

The suite contains 59 cumulative xUnit tests, well above the 10-test minimum. Tests cover factory success and every structured rejection rule, both `Result` cases, lookup presence and absence, `Map` and `Bind` success and failure propagation, the real analysis chain, short-circuit behavior, file errors, and batch and report behavior. The submitted project includes all tests and the five sample input files in `ThermodynamicsDataAnalyzer/Data/experiments`.

**Latest observed result (2026-10-23):** 59 total, 59 passed, 0 failed, 0 skipped. Verified with `dotnet test PhysicsDataAnalyzer.sln --no-restore -m:1 -nodeReuse:false` using SDK `10.0.400`.

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
| Result, factory, lookup, reader, pipeline, and physics tests | `ThermodynamicsDataAnalyzer.Tests/ResultTests.cs`, `ExperimentFactoryTests.cs`, `ExperimentCatalogTests.cs`, `ExperimentFileReaderTests.cs`, `ExperimentPipelineTests.cs`, `ExperimentBatchPipelineTests.cs`, `ExperimentValidatorTests.cs`, `ThermodynamicsCalculatorTests.cs`, `SpecificHeatEstimatorTests.cs`, `TestData.cs` |

## Submission archive

The submission archive contains the solution, application and test source, README, and sample input and report JSON. Generated `bin` and `obj` folders, IDE caches, and user-specific IDE settings are excluded. Restore packages and rebuild after extracting the archive.

## Author and contributions

| Name | Group | Concrete contribution |
|---|---|---|
| Ayan Rakhimov | IT-2513 | Implemented the experiment models and validation, thermodynamics calculations, both specific-heat policies, JSON dataset reader and report writer, batch pipeline and CLI, example experiment data, and xUnit coverage. This README describes the implemented workflow, requirements mapping, and commands. |
