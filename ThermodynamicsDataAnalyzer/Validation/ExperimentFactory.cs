using PhysicsDataAnalyzer.Models;

namespace PhysicsDataAnalyzer.Validation;

public static class ExperimentFactory
{
    public static Result<ValidatedExperiment, DomainError> Create(ExperimentInput? input)
    {
        if (input is null)
            return Failure(DomainError.Codes.InputRequired, "Experiment input is required.");

        if (string.IsNullOrWhiteSpace(input.ExperimentId))
            return Failure(DomainError.Codes.ExperimentIdRequired, "Experiment ID is required.");

        if (string.IsNullOrWhiteSpace(input.Material))
            return Failure(DomainError.Codes.MaterialRequired, $"Experiment '{input.ExperimentId}': material is required.");

        if (!IsPositiveFinite(input.MassKg))
            return Failure(DomainError.Codes.MassPositiveFinite, $"Experiment '{input.ExperimentId}': mass must be finite and greater than zero.");

        if (!IsPositiveFinite(input.HeaterPowerW))
            return Failure(DomainError.Codes.HeaterPowerPositiveFinite, $"Experiment '{input.ExperimentId}': heater power must be finite and greater than zero.");

        if (!IsPositiveFinite(input.SpecificHeatCapacityJPerKgK))
            return Failure(DomainError.Codes.ReferenceHeatCapacityPositiveFinite, $"Experiment '{input.ExperimentId}': reference heat capacity must be finite and greater than zero.");

        var measurements = input.Measurements;
        if (measurements is null || measurements.Count < 2)
            return Failure(DomainError.Codes.MeasurementsMinimum, $"Experiment '{input.ExperimentId}': at least two measurements are required.");

        var validatedMeasurements = new List<TemperatureMeasurement>(measurements.Count);
        for (int i = 0; i < measurements.Count; i++)
        {
            TemperatureMeasurement? measurement = measurements[i];
            if (measurement is null)
                return Failure(DomainError.Codes.MeasurementRequired, $"Experiment '{input.ExperimentId}': measurement {i} is null; every measurement is required.");

            if (!double.IsFinite(measurement.TimeSeconds) || !double.IsFinite(measurement.TemperatureCelsius))
                return Failure(DomainError.Codes.MeasurementFinite, $"Experiment '{input.ExperimentId}': measurement {i} time and temperature must be finite.");

            if (measurement.TimeSeconds < 0)
                return Failure(DomainError.Codes.MeasurementTimeNonNegative, $"Experiment '{input.ExperimentId}': measurement {i} time must be nonnegative.");

            if (i > 0 && measurement.TimeSeconds <= validatedMeasurements[i - 1].TimeSeconds)
                return Failure(DomainError.Codes.MeasurementTimeIncreasing, $"Experiment '{input.ExperimentId}': measurement {i} time must be greater than measurement {i - 1}.");

            validatedMeasurements.Add(measurement);
        }

        var validated = new ValidatedExperiment(input, validatedMeasurements);
        return new Result<ValidatedExperiment, DomainError>.Success(validated);
    }

    private static Result<ValidatedExperiment, DomainError> Failure(string code, string message) =>
        new Result<ValidatedExperiment, DomainError>.Failure(new DomainError(code, message));

    private static bool IsPositiveFinite(double value) => double.IsFinite(value) && value > 0;
}
