using PhysicsDataAnalyzer.Models;
using PhysicsDataAnalyzer.Physics;
using PhysicsDataAnalyzer.Validation;
using AnalysisPolicyType = PhysicsDataAnalyzer.Models.AnalysisPolicy;

namespace PhysicsDataAnalyzer.Pipeline;

public class ExperimentPipeline
{
    public static bool TryAnalyzeBatch(
        IReadOnlyList<ExperimentFileRecord>? records,
        AnalysisPolicyType policyType,
        AnalysisPolicy.SpecificHeatPolicy? policy,
        out string? errorMessage,
        out ExperimentBatchReport? report)
    {
        errorMessage = null;
        report = null;

        if (records is null)
        {
            errorMessage = "Dataset cannot be null";
            return false;
        }

        if (policy is null)
        {
            errorMessage = "Analysis policy is required";
            return false;
        }

        var accepted = new List<AcceptedExperiment>();
        var rejected = new List<RejectedExperiment>();

        foreach (ExperimentFileRecord record in records)
        {
            if (!string.IsNullOrWhiteSpace(record.ReadError))
            {
                rejected.Add(new RejectedExperiment(record.SourceFile, record.ReadError));
                continue;
            }

            if (!ExperimentValidator.TryValidate(
                    record.Input,
                    out var experiment,
                    out var message)
                || experiment is null)
            {
                rejected.Add(new RejectedExperiment(record.SourceFile, message ?? "Experiment validation failed"));
                continue;
            }

            if (policy(experiment, out double estimate))
            {
                rejected.Add(new RejectedExperiment(record.SourceFile, $"Policy type {policyType} could not calculate estimate"));
                continue;
            }
            
            accepted.Add(new AcceptedExperiment(record.SourceFile, CreateExperimentReport(experiment, policyType, estimate)));
        }

        report = new ExperimentBatchReport(policyType, records.Count, accepted.AsReadOnly(), rejected.AsReadOnly());

        return true;
    }

    public static bool TryAnalyze(
        ExperimentInput? input,
        AnalysisPolicyType policyType,
        AnalysisPolicy.SpecificHeatPolicy? policy,
        out string? errorMessage,
        out ExperimentReport? report)
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
        double percentageError = absoluteError / knownValue * 100;

        return new AnalysisResult(policy, estimate, knownValue, absoluteError, percentageError);
    }
    
    private static ExperimentReport CreateExperimentReport(
        ValidatedExperiment experiment,
        AnalysisPolicyType policyType,
        double estimate)
    {
        var measurements = experiment.Measurements;
        double initialTemperature = measurements[0].TemperatureCelsius;
        double finalTemperature = measurements[^1].TemperatureCelsius;
        double temperatureChange = finalTemperature - initialTemperature;
        double elapsedTime = measurements[^1].TimeSeconds - measurements[0].TimeSeconds;
        double totalEnergy = ThermodynamicsCalculator.CalculateEnergySupplied(
            experiment.HeaterPowerW,
            elapsedTime);

        AnalysisResult result = CreateResult(
            policyType,
            estimate,
            experiment.SpecificHeatCapacityJPerKgK);

        return new ExperimentReport(
            experiment.ExperimentId,
            experiment.Material,
            initialTemperature,
            finalTemperature,
            temperatureChange,
            elapsedTime,
            totalEnergy,
            result);
    }
}