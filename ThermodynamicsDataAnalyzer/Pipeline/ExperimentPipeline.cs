using PhysicsDataAnalyzer.Models;
using PhysicsDataAnalyzer.Physics;
using PhysicsDataAnalyzer.Validation;
using AnalysisPolicyType = PhysicsDataAnalyzer.Models.AnalysisPolicy;
using EstimatorPolicy = PhysicsDataAnalyzer.Pipeline.AnalysisPolicy;

namespace PhysicsDataAnalyzer.Pipeline;

public static class ExperimentPipeline
{
    public static bool TryAnalyzeBatch(
        IReadOnlyList<ExperimentFileRecord>? records,
        AnalysisPolicyType policy,
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

        if (SelectEstimator(policy) is null)
        {
            errorMessage = "Unknown analysis policy";
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

            var (acceptedEntry, rejectedEntry) = Analyze(record.Input, policy) switch
            {
                AnalysisOutcome.Success success => (
                    new AcceptedExperiment(record.SourceFile, success.Report),
                    (RejectedExperiment?)null),
                AnalysisOutcome.Failure failure => (
                    (AcceptedExperiment?)null,
                    new RejectedExperiment(record.SourceFile, string.Join(" ", failure.Reasons))),
                _ => throw new InvalidOperationException("Unknown analysis outcome")
            };

            if (acceptedEntry is not null)
                accepted.Add(acceptedEntry);
            if (rejectedEntry is not null)
                rejected.Add(rejectedEntry);
        }

        report = new ExperimentBatchReport(policy, records.Count, accepted.AsReadOnly(), rejected.AsReadOnly());
        return true;
    }

    public static AnalysisOutcome Analyze(ExperimentInput? input, AnalysisPolicyType policy)
    {
        var estimator = SelectEstimator(policy);
        if (estimator is null)
            return new AnalysisOutcome.Failure(new[] { "Unknown analysis policy" });

        ValidatedExperiment experiment;
        switch (ExperimentFactory.Create(input))
        {
            case Result<ValidatedExperiment, DomainError>.Success valid:
                experiment = valid.Value;
                break;
            case Result<ValidatedExperiment, DomainError>.Failure failure:
                return new AnalysisOutcome.Failure(new[] { failure.Error.Message });
            default:
                throw new InvalidOperationException("Unknown validation result");
        }

        if (!estimator(experiment, out double estimate))
            return new AnalysisOutcome.Failure(new[] { "The selected policy could not calculate estimate" });

        return new AnalysisOutcome.Success(CreateExperimentReport(experiment, policy, estimate));
    }

    public static AnalysisResult CreateResult(AnalysisPolicyType policy, double estimate, double knownValue)
    {
        double absoluteError = Math.Abs(estimate - knownValue);
        double percentageError = absoluteError / knownValue * 100;
        return new AnalysisResult(policy, estimate, knownValue, absoluteError, percentageError);
    }

    private static EstimatorPolicy.SpecificHeatPolicy? SelectEstimator(AnalysisPolicyType policy) => policy switch
    {
        AnalysisPolicyType.WholeExperiment => EstimatorPolicy.TryEstimateWholeExperimentPolicy,
        AnalysisPolicyType.MeanInterval => EstimatorPolicy.TryEstimateMeanIntervalPolicy,
        _ => null
    };

    private static ExperimentReport CreateExperimentReport(
        ValidatedExperiment experiment,
        AnalysisPolicyType policy,
        double estimate)
    {
        var measurements = experiment.Measurements;
        double initialTemperature = measurements[0].TemperatureCelsius;
        double finalTemperature = measurements[^1].TemperatureCelsius;
        double temperatureChange = finalTemperature - initialTemperature;
        double elapsedTime = measurements[^1].TimeSeconds - measurements[0].TimeSeconds;
        double totalEnergy = ThermodynamicsCalculator.CalculateEnergySupplied(experiment.HeaterPowerW, elapsedTime);

        AnalysisResult result = CreateResult(policy, estimate, experiment.SpecificHeatCapacityJPerKgK);
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
