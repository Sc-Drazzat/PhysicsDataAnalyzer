namespace PhysicsDataAnalyzer.Models;
public sealed record DomainError
{
    public static class Codes
    {
        public const string InputRequired = "experiment.input.required";
        public const string ExperimentIdRequired = "experiment.id.required";
        public const string MaterialRequired = "experiment.material.required";
        public const string MassPositiveFinite = "experiment.mass.positive_finite";
        public const string HeaterPowerPositiveFinite = "experiment.heater_power.positive_finite";
        public const string ReferenceHeatCapacityPositiveFinite = "experiment.reference_heat_capacity.positive_finite";
        public const string MeasurementsMinimum = "experiment.measurements.minimum";
        public const string MeasurementRequired = "experiment.measurement.required";
        public const string MeasurementFinite = "experiment.measurement.finite";
        public const string MeasurementTimeNonNegative = "experiment.measurement.time.nonnegative";
        public const string MeasurementTimeIncreasing = "experiment.measurement.time.increasing";
        public const string ExperimentNotFound = "experiment.lookup.not_found";
        public const string AnalysisPolicyInvalid = "analysis.policy.invalid";
        public const string CalculationRejected = "analysis.calculation.rejected";
        public const string FilePathRequired = "io.file.path.required";
        public const string FileNotFound = "io.file.not_found";
        public const string FileJsonInvalid = "io.file.json.invalid";
        public const string FileReadFailed = "io.file.read.failed";
        public const string FileAccessDenied = "io.file.access.denied";
        public const string DatasetPathRequired = "io.dataset.path.required";
        public const string DatasetNotFound = "io.dataset.not_found";
        public const string DatasetReadFailed = "io.dataset.read.failed";
        public const string DatasetAccessDenied = "io.dataset.access.denied";
        public const string AnalysisBatchDatasetRequired = "analysis.batch.dataset.required";
    }

    public string Code { get; }
    public string Message { get; }

    public DomainError(string code, string message)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("An error code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("An error message is required.", nameof(message));

        Code = code;
        Message = message;
    }
}
