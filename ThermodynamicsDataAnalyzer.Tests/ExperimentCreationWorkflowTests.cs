using System.Text.Json;
using PhysicsDataAnalyzer.Models;
using PhysicsDataAnalyzer.Pipeline;

namespace PhysicsDataAnalyzer.Tests;

public class ExperimentCreationWorkflowTests
{
    [Fact]
    public void CreatesAndSavesAValidExperiment()
    {
        WithTempDirectory(directory =>
        {
            string path = Path.Combine(directory, "created.json");

            var result = ExperimentCreationWorkflow.Create(TestData.ValidExperiment(), path);

            var savedPath = Assert.IsType<Result<string, DomainError>.Success>(result).Value;
            Assert.Equal(path, savedPath);
            var saved = JsonSerializer.Deserialize<ExperimentInput>(File.ReadAllText(path));
            Assert.NotNull(saved);
            Assert.Equal("test-001", saved.ExperimentId);
            Assert.Equal(TestData.ValidExperiment().Measurements!.Count, saved.Measurements!.Count);
        });
    }

    [Fact]
    public void RejectsInvalidExperimentWithoutWritingAFile()
    {
        WithTempDirectory(directory =>
        {
            string path = Path.Combine(directory, "invalid.json");

            var result = ExperimentCreationWorkflow.Create(TestData.ValidExperiment(massKg: 0), path);

            var error = Assert.IsType<Result<string, DomainError>.Failure>(result).Error;
            Assert.Equal(DomainError.Codes.MassPositiveFinite, error.Code);
            Assert.False(File.Exists(path));
        });
    }

    [Fact]
    public void ReturnsWriteFailureWhenOutputParentIsAFile()
    {
        WithTempDirectory(directory =>
        {
            string parentFile = Path.Combine(directory, "not-a-directory");
            File.WriteAllText(parentFile, "occupied");
            string path = Path.Combine(parentFile, "experiment.json");

            var result = ExperimentCreationWorkflow.Create(TestData.ValidExperiment(), path);

            var error = Assert.IsType<Result<string, DomainError>.Failure>(result).Error;
            Assert.Equal(DomainError.Codes.FileWriteFailed, error.Code);
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
