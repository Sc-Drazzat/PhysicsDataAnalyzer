using PhysicsDataAnalyzer.Models;

namespace PhysicsDataAnalyzer.Validation;

public static class ExperimentFactory
{
    public static ValidationOutcome Create(ExperimentInput? input)
    {
        if (input is null)
        {
            return new ValidationOutcome.Invalid(new[] { "Experiment cannot be null" });
        }

        var reasons = new List<string>();
        
        if (string.IsNullOrWhiteSpace(input.ExperimentId))
        {
            reasons.Add("Experiment Id can not be empty");
        }

        if (string.IsNullOrWhiteSpace(input.Material))
        { 
            reasons.Add("Experiment material can not be empty");
        }

        if (input.MassKg <= 0 || !double.IsFinite(input.MassKg))
        {
            reasons.Add("Material mass must be more than zero");
        }

        if (input.HeaterPowerW <= 0 || !double.IsFinite(input.HeaterPowerW))
        {
            reasons.Add("Heater power must be more than zero");
        }

        if (input.SpecificHeatCapacityJPerKgK <= 0 || !double.IsFinite(input.SpecificHeatCapacityJPerKgK))
        {
            reasons.Add("Heat capacity must be  more than zero");
        }

        var measurements = input.Measurements;
        var validatedMeasurements = new List<TemperatureMeasurement>(measurements?.Count ?? 0);
        if (measurements == null || measurements.Count < 2)
        {
            reasons.Add("Measurements count must be at least 2");
        }
        else
        {
            for (int i = 0; i < measurements.Count; i++)
            {
                TemperatureMeasurement? measurement = measurements[i];

                if (measurement is null)
                {
                    reasons.Add($"Measurement {i} cannot contain null entries");
                    continue;
                }

                if (!double.IsFinite(measurement.TimeSeconds)
                    || !double.IsFinite(measurement.TemperatureCelsius))
                {
                    reasons.Add($"Measurement {i} must have finite times and temperatures");
                    continue;
                }

                if (measurement.TimeSeconds < 0)
                {
                    reasons.Add($"Measurement {i} time cannot be negative");
                }

                if (i > 0
                    && measurements[i - 1] is { } previous
                    && double.IsFinite(previous.TimeSeconds)
                    && double.IsFinite(measurement.TimeSeconds)
                    && measurement.TimeSeconds <= previous.TimeSeconds)
                {
                    reasons.Add($"Measurement {i} time must increase");
                }

                validatedMeasurements.Add(measurement);
            }
        }

        if (reasons.Count > 0)
        {
            return new ValidationOutcome.Invalid(Array.AsReadOnly(reasons.ToArray()));
        }

        var validated = new ValidatedExperiment(input, validatedMeasurements);

        return new ValidationOutcome.Valid(validated);
    }
}
