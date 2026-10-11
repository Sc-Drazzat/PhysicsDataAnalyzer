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

        if (input.MassKg <= 0)
        {
            errorMessage = "Material mass must be more than zero";
            return false;
        }

        if (input.HeaterPowerW <= 0)
        {
            errorMessage = "Heater power must be more than zero";
            return false;
        }

        if (input.SpecificHeatCapacityJPerKgK <= 0)
        {
            errorMessage = "Heat capacity must be  more than zero";
            return false;
        }

        if (input.Measurements == null || input.Measurements.Count < 2)
        {
            errorMessage = "Measurements count must be at least 2";
            return false;
        }
        
        for (int i = 1; i < input.Measurements.Count; i++)
        {
            if (input.Measurements[i].TimeSeconds <= input.Measurements[i - 1].TimeSeconds)
            {
                errorMessage = "Measurements time must increase";
                return false;
            }
        }
        
        experiment = new ValidatedExperiment(input);
        return true;
    }
}