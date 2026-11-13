using PhysicsDataAnalyzer.Models;
using PhysicsDataAnalyzer.Pipeline;
using AnalysisPolicyType = PhysicsDataAnalyzer.Models.AnalysisPolicy;

namespace PhysicsDataAnalyzer.Tests;

public class SingleExperimentAnalysisWorkflowTests
{
    [Fact]
    public void AnalyzesRequestedExperimentFromDataset()
    {
        WithTempDirectory(directory =>
        {
            File.WriteAllText(Path.Combine(directory, "target.json"),
                System.Text.Json.JsonSerializer.Serialize(TestData.ValidExperiment()));
            File.WriteAllText(Path.Combine(directory, "other.json"),
                System.Text.Json.JsonSerializer.Serialize(TestData.ValidExperiment() with { ExperimentId = "other" }));

            var result = SingleExperimentAnalysisWorkflow.AnalyzeDataset(directory, "test-001", AnalysisPolicyType.WholeExperiment);

            var report = Assert.IsType<Result<ExperimentReport, DomainError>.Success>(result).Value;
            Assert.Equal("test-001", report.ExperimentId);
            Assert.Equal(AnalysisPolicyType.WholeExperiment, report.Result.Policy);
        });
    }

    [Fact]
    public void ReturnsNotFoundForAnUnknownExperimentId()
    {
        WithTempDirectory(directory =>
        {
            File.WriteAllText(Path.Combine(directory, "experiment.json"),
                System.Text.Json.JsonSerializer.Serialize(TestData.ValidExperiment()));

            var result = SingleExperimentAnalysisWorkflow.AnalyzeDataset(directory, "missing", AnalysisPolicyType.WholeExperiment);

            var error = Assert.IsType<Result<ExperimentReport, DomainError>.Failure>(result).Error;
            Assert.Equal(DomainError.Codes.ExperimentNotFound, error.Code);
            Assert.Contains("missing", error.Message);
        });
    }

    [Fact]
    public void ReturnsValidationFailureForAnInvalidExperiment()
    {
        WithTempDirectory(directory =>
        {
            File.WriteAllText(Path.Combine(directory, "invalid.json"),
                System.Text.Json.JsonSerializer.Serialize(TestData.ValidExperiment(massKg: 0)));

            var result = SingleExperimentAnalysisWorkflow.AnalyzeDataset(directory, "test-001", AnalysisPolicyType.WholeExperiment);

            var error = Assert.IsType<Result<ExperimentReport, DomainError>.Failure>(result).Error;
            Assert.Equal(DomainError.Codes.MassPositiveFinite, error.Code);
        });
    }

    private static void WithTempDirectory(Action<string> action)
    {
        string directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try { action(directory); }
        finally { Directory.Delete(directory, recursive: true); }
    }
}
