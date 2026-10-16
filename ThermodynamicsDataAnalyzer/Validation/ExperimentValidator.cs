using System.ComponentModel.DataAnnotations;
using PhysicsDataAnalyzer.Models;

namespace PhysicsDataAnalyzer.Validation;

public class ExperimentValidator
{
    public static bool TryValidate(ExperimentInput? input, out ValidatedExperiment? experiment, out string? errorMessage)
    {
        experiment = null;
        errorMessage = null;

        if (input == null)
        {
            errorMessage = "Experiment Input can not be null";
            return false;
        }

        if (string.IsNullOrWhiteSpace(input.ExperimentId))
        {
            errorMessage = "Experiment Id can not be empty";
            return false;
        }

        if (string.IsNullOrWhiteSpace(input.Material))
        {
            errorMessage = "Experiment material can not be empty";
            return false;
        }

        if (input.MassKg <= 0 || !double.IsFinite(input.MassKg))
        {
            errorMessage = "Material mass must be more than zero";
            return false;
        }

        if (input.HeaterPowerW <= 0 || !double.IsFinite(input.HeaterPowerW))
        {
            errorMessage = "Heater power must be more than zero";
            return false;
        }

        if (input.SpecificHeatCapacityJPerKgK <= 0 || !double.IsFinite(input.SpecificHeatCapacityJPerKgK))
        {
            errorMessage = "Heat capacity must be  more than zero";
            return false;
        }

        var measurements = input.Measurements;
        if (measurements == null || measurements.Count < 2)
        {
            errorMessage = "Measurements count must be at least 2";
            return false;
        }

        var validatedMeasurements = new List<TemperatureMeasurement>(measurements.Count);
        for (int i = 0; i < measurements.Count; i++)
        {
            TemperatureMeasurement? measurement = measurements[i];

            if (measurement is null)
            {
                errorMessage = "Measurements cannot contain null entries";
                return false;
            }

            if (!double.IsFinite(measurement.TimeSeconds)
                || !double.IsFinite(measurement.TemperatureCelsius))
            {
                errorMessage = "Measurements must have finite times and temperatures";
                return false;
            }

            if (i > 0
                && measurement.TimeSeconds <= validatedMeasurements[i - 1].TimeSeconds)
            {
                errorMessage = "Measurements time must increase";
                return false;
            }

            validatedMeasurements.Add(measurement);
        }

        experiment = new ValidatedExperiment(input, validatedMeasurements);
        return true;
    }
}
