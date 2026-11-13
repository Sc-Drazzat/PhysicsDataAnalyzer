using PhysicsDataAnalyzer.IO;
using PhysicsDataAnalyzer.Models;
using AnalysisPolicyType = PhysicsDataAnalyzer.Models.AnalysisPolicy;

namespace PhysicsDataAnalyzer.Pipeline;

public static class SingleExperimentAnalysisWorkflow
{
    public static Result<ExperimentReport, DomainError> AnalyzeDataset(
        string? datasetDirectory,
        string experimentId,
        AnalysisPolicyType policy)
    {
        switch (ExperimentFileReader.TryReadDataset(datasetDirectory))
        {
            case Result<IReadOnlyList<ExperimentFileRecord>, DomainError>.Failure failure:
                return new Result<ExperimentReport, DomainError>.Failure(failure.Error);
            case Result<IReadOnlyList<ExperimentFileRecord>, DomainError>.Success success:
                return ExperimentPipeline.Analyze(new ExperimentCatalog(success.Value), experimentId, policy);
            default:
                throw new InvalidOperationException("Unknown dataset read result");
        }
    }
}
