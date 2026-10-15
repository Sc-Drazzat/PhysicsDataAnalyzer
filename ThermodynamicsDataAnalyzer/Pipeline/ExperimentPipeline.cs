using PhysicsDataAnalyzer.Models;
using PhysicsDataAnalyzer.Physics;
using PhysicsDataAnalyzer.Validation;

using AnalysisPolicyType = PhysicsDataAnalyzer.Models.AnalysisPolicy;

namespace PhysicsDataAnalyzer.Pipeline;

public class ExperimentPipeline
{
    public static bool TryAnalyze(ExperimentInput? input, AnalysisPolicyType policyType, AnalysisPolicy.SpecificHeatPolicy? policy, out string? errorMessage, out ExperimentReport? report)
    {
        errorMessage = null;
        report = null;

        if (!ExperimentValidator.TryValidate(input, out var experiment, out errorMessage))
        {
            return false;
        }

        if (experiment == null)
        {
            errorMessage = "Validation did not produced experiment";
            return false;
        }

        if (policy == null)
        {
            errorMessage = "Police cannot be null";
            return false;
        }

        if (!policy(experiment, out double estimate))
        {
            errorMessage = "Could not calculate policy";
            return false;
        }
        
        double knownValue = experiment.SpecificHeatCapacityJPerKgK;
        
        var measurements = experiment.Measurements;
        double initialTemperature = measurements.First().TemperatureCelsius;
        double finalTemperature = measurements.Last().TemperatureCelsius;
        double temperatureChange = finalTemperature - initialTemperature;
        double elapsedTime = measurements.Last().TimeSeconds - measurements.First().TimeSeconds;
        double totalEnergy = ThermodynamicsCalculator.CalculateEnergySupplied(experiment.HeaterPowerW, elapsedTime);

        var result = CreateResult(policyType, estimate, knownValue);
        report = new ExperimentReport(
            experiment.ExperimentId,
            experiment.Material,
            initialTemperature,
            finalTemperature,
            temperatureChange,
            elapsedTime,
            totalEnergy,
            result);
        return true;
    }

    public static AnalysisResult CreateResult(AnalysisPolicyType policy, double estimate, double knownValue)
    {
        double absoluteError = Math.Abs(estimate - knownValue);
        double percentageError = absoluteError/knownValue*100;
        
        return new AnalysisResult(policy, estimate, knownValue, absoluteError, percentageError);
    }
}