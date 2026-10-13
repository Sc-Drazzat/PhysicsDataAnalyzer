using PhysicsDataAnalyzer.Models;
using PhysicsDataAnalyzer.Physics;
using PhysicsDataAnalyzer.Validation;

using AnalysisPolicyType = PhysicsDataAnalyzer.Models.AnalysisPolicy;

namespace PhysicsDataAnalyzer.Pipeline;

public class ExperimentPipeline
{
    public static bool TryAnalyze(ExperimentInput? input, out string? errorMessage, out ExperimentReport? report)
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

        if (!AnalysisPolicy.TryEstimateWholeExperimentPolicy(experiment, out var wholeExperimentEstimation))
        {
            errorMessage = "Could not calculate whole experiment estimation";
            return false;
        }

        if (!AnalysisPolicy.TryEstimateMeanIntervalPolicy(experiment, out var meanIntervalEstimation))
        {
            errorMessage = "Could not calculate mean interval experiment estimation";
            return false;
        }

        double knownValue = experiment.SpecificHeatCapacityJPerKgK;
        
        var measurements = experiment.Measurements;
        double initialTemperature = measurements.First().TemperatureCelsius;
        double finalTemperature = measurements.Last().TemperatureCelsius;
        double temperatureChange = finalTemperature - initialTemperature;
        double elapsedTime = measurements.Last().TimeSeconds - measurements.First().TimeSeconds;
        double totalEnergy = ThermodynamicsCalculator.CalculateEnergySupplied(experiment.HeaterPowerW, elapsedTime);

        report = new ExperimentReport(
            experiment.ExperimentId,
            experiment.Material,
            initialTemperature,
            finalTemperature,
            temperatureChange,
            elapsedTime,
            totalEnergy,
            CreateResult(
                AnalysisPolicyType.WholeExperiment,
                wholeExperimentEstimation,
                knownValue
                ),
            CreateResult(
                AnalysisPolicyType.MeanInterval,
                meanIntervalEstimation,
                knownValue
                )
            );
        return true;
    }

    public static AnalysisResult CreateResult(AnalysisPolicyType policy, double estimate, double knownValue)
    {
        double absoluteError = Math.Abs(estimate - knownValue);
        double percentageError = absoluteError/knownValue*100;
        
        return new AnalysisResult(policy, estimate, knownValue, absoluteError, percentageError);
    }
}