# Thermodynamics Data Analyzer

## Purpose and research question

This .NET console application analyzes temperature measurements from a constant-power heating experiment. It estimates a material's specific heat capacity from the measured heating data and compares that estimate with a supplied reference value.

**Research question:** How closely does the apparent specific heat capacity calculated from heater energy and measured temperature change agree with the reference specific heat capacity?

The estimate is *apparent*: the current model treats all energy supplied by the heater as energy absorbed by the sample.

## Input format and units

Input is a JSON object with these fields:

| Field | Type | Unit / meaning |
|---|---|---|
| `experimentId` | string | Experiment identifier; required, nonblank |
| `material` | string | Material name; required, nonblank |
| `massKg` | number | Sample mass in kilograms; must be greater than zero |
| `heaterPowerW` | number | Constant heater power in watts; must be greater than zero |
| `specificHeatCapacityJPerKgK` | number | Reference specific heat capacity in J/(kg·K); must be greater than zero |
| `measurements` | array | At least two measurement objects |
| `timeSeconds` | number | Measurement time in seconds, strictly increasing |
| `temperatureCelsius` | number | Measured temperature in degrees Celsius |

Example: [`Data/sample-experiment.json`](Data/sample-experiment.json).

Property names are read case-insensitively. A missing file, unreadable file, malformed JSON, or JSON `null` is reported as an input error. A syntactically valid object missing required constructor data may fail during deserialization or validation.

## Physics, assumptions, and policies

The calculations use:

- Elapsed time: `Δt = t_final − t_initial` (s)
- Temperature change: `ΔT = T_final − T_initial` (°C difference, numerically equal to K difference)
- Supplied energy: `Q = P × Δt` (J), where `P` is in watts
- Whole-experiment apparent specific heat: `c = Q / (m × ΔT)` (J/(kg·K))
- Per-interval apparent specific heat: `cᵢ = Qᵢ / (m × ΔTᵢ)`, with `Qᵢ = P × Δtᵢ`
- Mean-interval estimate: arithmetic mean of all `cᵢ`
- Reported error: absolute difference `|c_est − c_reference|`; percentage `|c_est − c_reference| / c_reference × 100%`

The model assumes constant heater power and does not correct for heat loss, heating of the container or sensor, or changes in heater power. The mean-interval policy requires each interval to have a positive temperature change; otherwise analysis fails. The pipeline currently calculates and reports both policies for every accepted experiment. Although a policy delegate type is declared in `Pipeline/AnalysisPolicy.cs`, the current pipeline does not accept a caller-selected policy.

## Domain validation

`ExperimentValidator.TryValidate` rejects null input, blank experiment ID or material, nonpositive mass, heater power, or reference heat capacity, fewer than two measurements, and timestamps that are duplicate or decreasing. The estimator rejects nonpositive mass or temperature change and nonfinite calculated estimates. Nonincreasing timestamps are rejected before calculation.

The validator does not currently check that numeric inputs and measurements are finite, nor that measurement temperatures increase overall. Consequently, some nonfinite values may pass initial validation and then fail or produce invalid calculations later. The mean-interval estimate specifically fails if any interval has zero or negative temperature change. Errors are returned through the `Try...` APIs and the console app exits with a nonzero status.

## Pipeline and output

The workflow is:

1. `Program.cs` chooses an input path and output path.
2. `IO/ExperimentFileReader.cs` reads and deserializes JSON.
3. `Validation/ExperimentValidator.cs` validates and creates a `ValidatedExperiment` snapshot.
4. `Physics/ThermodynamicsCalculator.cs` calculates total elapsed time, temperature change, and supplied energy. `Physics/IntervalCalculator.cs` calculates each interval and its supplied energy.
5. `Pipeline/AnalysisPolicy.cs` applies the whole-experiment and mean-interval estimators in `Physics/HeatCapacityEstimator.cs`.
6. `Pipeline/ExperimentPipeline.cs` creates the report and computes absolute and percentage errors.
7. The report is printed to stdout and written as indented JSON by `IO/ReportWriter.cs`.

With no arguments, the application locates the sample JSON and writes `report.json` beside it. Arguments are `INPUT_JSON [OUTPUT_JSON]`; if the second argument is omitted, the report is written beside the chosen input. Output directories are created when needed.

For the included sample (0.2 kg water, 100 W, measurements from 20.0 °C at 0 s to 31.0 °C at 100 s): elapsed time is 100 s, temperature change is 11 K, and supplied energy is 10,000 J. Both policies estimate approximately 4545.45 J/(kg·K); the reference is 4180 J/(kg·K), with absolute error approximately 365.45 J/(kg·K) and percentage error approximately 8.74%. The checked-in sample report is [`Data/report.json`](Data/report.json). Small floating-point differences between policies are expected.

## Build and run

The projects target `net10.0`. The SDK used for this submission check was **10.0.400**. From the repository root:

```bash
dotnet restore PhysicsDataAnalyzer.sln
dotnet build PhysicsDataAnalyzer.sln
dotnet run --project ThermodynamicsDataAnalyzer/ThermodynamicsDataAnalyzer.csproj
```

Run with explicit paths (the output path is optional):

```bash
dotnet run --project ThermodynamicsDataAnalyzer/ThermodynamicsDataAnalyzer.csproj -- ThermodynamicsDataAnalyzer/Data/sample-experiment.json /tmp/water-report.json
```

To demonstrate a failure caused by a missing input file:

```bash
dotnet run --project ThermodynamicsDataAnalyzer/ThermodynamicsDataAnalyzer.csproj -- /tmp/does-not-exist.json
```

This prints an `Input error` and exits with a nonzero status. To demonstrate a domain validation failure, make a copy of the sample JSON with `massKg` set to `0` and pass that file as the input; the pipeline reports an analysis error and does not create a report.

## Automated tests and latest result

The test project is xUnit. Run it from the repository root:

```bash
dotnet test PhysicsDataAnalyzer.sln
```

Tests cover accepted and invalid experiments, missing text values, nonpositive mass, too few measurements, duplicate/decreasing timestamps, time and temperature differences, energy calculations, interval construction, both estimators, zero/negative interval temperature change, full report creation, pipeline error handling, and preservation of the input measurements.

**Latest observed result:** 19 total, 19 passed, 0 failed, 0 skipped. 

## Requirement-to-file and method mapping

| Requirement | Implemented location |
|---|---|
| Console entry point and default sample/output paths | `ThermodynamicsDataAnalyzer/Program.cs` — top-level program, `FindSampleInputPath` |
| JSON input | `IO/ExperimentFileReader.cs` — `TryReadExperiment` |
| Input data models | `Models/ExperimentInput.cs`, `Models/TemperatureMeasurement.cs` |
| Domain validation and validated snapshot | `Validation/ExperimentValidator.cs` — `TryValidate`; `Models/ValidatedExperiment.cs` |
| Time, temperature, and energy calculations | `Physics/ThermodynamicsCalculator.cs` — `CalculateElapsedTimeSeconds`, `CalculateTemperatureChangeCelsius`, `CalculateEnergySupplied`, `CalculateIntervalEnergySupplied` |
| Interval construction | `Physics/IntervalCalculator.cs` — `CalculateIntervals`; `Models/CalculatedInterval.cs` |
| Specific heat estimators | `Physics/HeatCapacityEstimator.cs` — `TryEstimateWholeExperiment`, `TryEstimateMeanInterval` |
| Policy wrappers | `Pipeline/AnalysisPolicy.cs` — `TryEstimateWholeExperimentPolicy`, `TryEstimateMeanIntervalPolicy` |
| Workflow and error/percentage values | `Pipeline/ExperimentPipeline.cs` — `TryAnalyze`, `CreateResult` |
| Report model and JSON output | `Models/ExperimentReport.cs`, `Models/AnalysisResult.cs`, `IO/ReportWriter.cs` — `TryWriteReport` |
| Validation, physics, estimator, and pipeline tests | `ThermodynamicsDataTests/ExperimentValidatorTests.cs`, `ThermodynamicsCalculatorTests.cs`, `SpecificHeatEstimatorTests.cs`, `ExperimentPipelineTests.cs`, `TestData.cs` |

## Author and contributions

| Name | Group | Concrete contribution |
|---|---|---|
| Ayan Rakhimov | IT-2513 | The working tree contains the console workflow, JSON reader/writer, models, validation, physics calculations, both estimators, sample data/report, and xUnit coverage listed above. I documented the implemented behavior, file/method mapping, commands, and observed test outcome in this README. The available repository metadata does not establish who authored each source/test file, so individual implementation and test authorship should be confirmed by the project member before submission. |
