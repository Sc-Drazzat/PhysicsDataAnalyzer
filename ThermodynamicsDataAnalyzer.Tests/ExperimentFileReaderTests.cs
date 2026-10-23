using System.Text.Json;
using PhysicsDataAnalyzer.IO;
using PhysicsDataAnalyzer.Models;

namespace PhysicsDataAnalyzer.Tests;

public class ExperimentFileReaderTests
{
    [Fact]
    public void ReadsAnExistingEmptyDirectoryAsAnEmptyDataset()
    {
        WithTempDirectory(directory =>
        {
            var result = ExperimentFileReader.TryReadDataset(directory);

            var records = Assert.IsType<Result<IReadOnlyList<ExperimentFileRecord>, DomainError>.Success>(result).Value;
            Assert.Empty(records);
        });
    }

    [Fact]
    public void ReportsMissingFileWithOperationAndPath()
    {
        WithTempDirectory(directory =>
        {
            string path = Path.Combine(directory, "missing.json");

            var result = ExperimentFileReader.TryReadExperiment(path);

            var error = Assert.IsType<Result<ExperimentInput, DomainError>.Failure>(result).Error;
            Assert.Equal(DomainError.Codes.FileNotFound, error.Code);
            Assert.Contains("Read experiment file", error.Message);
            Assert.Contains(path, error.Message);
        });
    }

    [Fact]
    public void ReportsMalformedJsonWithOperationAndPath()
    {
        WithTempDirectory(directory =>
        {
            string path = Path.Combine(directory, "broken.json");
            File.WriteAllText(path, "{ this is not valid json");

            var result = ExperimentFileReader.TryReadExperiment(path);

            var error = Assert.IsType<Result<ExperimentInput, DomainError>.Failure>(result).Error;
            Assert.Equal(DomainError.Codes.FileJsonInvalid, error.Code);
            Assert.Contains("Parse experiment file", error.Message);
            Assert.Contains(path, error.Message);
        });
    }

    [Fact]
    public void ReportsMissingOrBlankDatasetDirectory()
    {
        var blankResult = ExperimentFileReader.TryReadDataset(" ");
        var missingPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var missingResult = ExperimentFileReader.TryReadDataset(missingPath);

        var blankError = Assert.IsType<Result<IReadOnlyList<ExperimentFileRecord>, DomainError>.Failure>(blankResult).Error;
        var missingError = Assert.IsType<Result<IReadOnlyList<ExperimentFileRecord>, DomainError>.Failure>(missingResult).Error;
        Assert.Equal(DomainError.Codes.DatasetPathRequired, blankError.Code);
        Assert.Contains("Read dataset", blankError.Message);
        Assert.Equal(DomainError.Codes.DatasetNotFound, missingError.Code);
        Assert.Contains(missingPath, missingError.Message);
    }

    [Fact]
    public void MalformedFileFailsDatasetReadWithoutReturningPartialRecords()
    {
        WithTempDirectory(directory =>
        {
            string validPath = Path.Combine(directory, "a-valid.json");
            string invalidPath = Path.Combine(directory, "b-broken.json");
            File.WriteAllText(validPath, JsonSerializer.Serialize(TestData.ValidExperiment()));
            File.WriteAllText(invalidPath, "{ this is not valid json");

            var result = ExperimentFileReader.TryReadDataset(directory);

            var error = Assert.IsType<Result<IReadOnlyList<ExperimentFileRecord>, DomainError>.Failure>(result).Error;
            Assert.Equal(DomainError.Codes.FileJsonInvalid, error.Code);
            Assert.Contains("Parse experiment file", error.Message);
            Assert.Contains(invalidPath, error.Message);
        });
    }

    [Fact]
    public void ReadsEveryValidFileInSortedOrder()
    {
        WithTempDirectory(directory =>
        {
            File.WriteAllText(
                Path.Combine(directory, "b-second.json"),
                JsonSerializer.Serialize(TestData.ValidExperiment() with { ExperimentId = "second" }));
            File.WriteAllText(
                Path.Combine(directory, "a-first.json"),
                JsonSerializer.Serialize(TestData.ValidExperiment() with { ExperimentId = "first" }));

            var result = ExperimentFileReader.TryReadDataset(directory);

            var records = Assert.IsType<Result<IReadOnlyList<ExperimentFileRecord>, DomainError>.Success>(result).Value;
            Assert.Equal(new[] { "a-first.json", "b-second.json" }, records.Select(record => record.SourceFile));
            Assert.Equal(new[] { "first", "second" }, records.Select(record => record.Input!.ExperimentId));
        });
    }

    private static void WithTempDirectory(Action<string> action)
    {
        string directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            action(directory);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
