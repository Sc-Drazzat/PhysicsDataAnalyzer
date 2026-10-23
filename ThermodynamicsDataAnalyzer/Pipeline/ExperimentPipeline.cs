using PhysicsDataAnalyzer.Models;
using PhysicsDataAnalyzer.Physics;
using PhysicsDataAnalyzer.Validation;
using AnalysisPolicyType = PhysicsDataAnalyzer.Models.AnalysisPolicy;
using EstimatorPolicy = PhysicsDataAnalyzer.Pipeline.AnalysisPolicy;

namespace PhysicsDataAnalyzer.Pipeline;

public static class ExperimentPipeline
{
    private sealed record AnalysisData(ValidatedExperiment Experiment, double Estimate);

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

        var estimator = SelectEstimator(policy);
        if (estimator is null)
        {
            errorMessage = new DomainError(DomainError.Codes.AnalysisPolicyInvalid, "Unknown analysis policy").Message;
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

            var catalog = new ExperimentCatalog(new[] { record });
            var experimentId = record.Input?.ExperimentId ?? string.Empty;
            switch (Analyze(catalog, experimentId, policy, estimator))
            {
                case Result<ExperimentReport, DomainError>.Success success:
                    accepted.Add(new AcceptedExperiment(record.SourceFile, success.Value));
                    break;
                case Result<ExperimentReport, DomainError>.Failure failure:
                    rejected.Add(new RejectedExperiment(record.SourceFile, failure.Error.Message));
                    break;
                default:
                    throw new InvalidOperationException("Unknown analysis result");
            }
        }

        report = new ExperimentBatchReport(policy, records.Count, accepted.AsReadOnly(), rejected.AsReadOnly());
        return true;
    }

    public static Result<ExperimentReport, DomainError> Analyze(
        ExperimentCatalog catalog,
        string experimentId,
        AnalysisPolicyType policy)
    {
        var estimator = SelectEstimator(policy);
        if (estimator is null)
            return new Result<ExperimentReport, DomainError>.Failure(
                new DomainError(DomainError.Codes.AnalysisPolicyInvalid, "Unknown analysis policy"));

        return Analyze(catalog, experimentId, policy, estimator);
    }

    public static Result<ExperimentReport, DomainError> Analyze(
        ExperimentCatalog catalog,
        string experimentId,
        AnalysisPolicyType policy,
        EstimatorPolicy.SpecificHeatPolicy estimator)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(experimentId);
        ArgumentNullException.ThrowIfNull(estimator);

        return Lookup(catalog, experimentId)
            .Bind(ExperimentFactory.Create)
            .Bind(experiment => Calculate(experiment, estimator))
            .Map(data => CreateExperimentReport(data.Experiment, policy, data.Estimate));
    }

    public static AnalysisResult CreateResult(AnalysisPolicyType policy, double estimate, double knownValue)
    {
        double absoluteError = Math.Abs(estimate - knownValue);
        double percentageError = absoluteError / knownValue * 100;
        return new AnalysisResult(policy, estimate, knownValue, absoluteError, percentageError);
    }

    private static Result<ExperimentInput, DomainError> Lookup(ExperimentCatalog catalog, string experimentId) =>
        catalog.FindById(experimentId) switch
        {
            Maybe<ExperimentFileRecord>.Some(var record) when record.Input is { } input =>
                new Result<ExperimentInput, DomainError>.Success(input),
            _ => new Result<ExperimentInput, DomainError>.Failure(
                new DomainError(DomainError.Codes.ExperimentNotFound, $"Experiment '{experimentId}' was not found."))
        };

    private static Result<AnalysisData, DomainError> Calculate(
        ValidatedExperiment experiment,
        EstimatorPolicy.SpecificHeatPolicy estimator)
    {
        if (!estimator(experiment, out double estimate) || !double.IsFinite(estimate) || estimate <= 0)
            return new Result<AnalysisData, DomainError>.Failure(
                new DomainError(DomainError.Codes.CalculationRejected,
                    $"The selected policy could not calculate a positive finite estimate for experiment '{experiment.ExperimentId}'."));

        return new Result<AnalysisData, DomainError>.Success(new AnalysisData(experiment, estimate));
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
