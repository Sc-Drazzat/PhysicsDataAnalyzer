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
            bool success = ExperimentFileReader.TryReadDataset(directory, out var records, out var error);

            Assert.True(success);
            Assert.Null(error);
            Assert.Empty(records);
        });
    }

    [Fact]
    public void RejectsMissingOrBlankDatasetDirectory()
    {
        bool blankSuccess = ExperimentFileReader.TryReadDataset(" ", out var blankRecords, out var blankError);
        bool missingSuccess = ExperimentFileReader.TryReadDataset(
            Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")),
            out var missingRecords,
            out var missingError);

        Assert.False(blankSuccess);
        Assert.Empty(blankRecords);
        Assert.NotNull(blankError);
        Assert.False(missingSuccess);
        Assert.Empty(missingRecords);
        Assert.Contains("not found", missingError, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void KeepsMalformedFileAsARecordAndContinuesReadingValidFiles()
    {
        WithTempDirectory(directory =>
        {
            File.WriteAllText(Path.Combine(directory, "a-broken.json"), "{ this is not valid json");
            File.WriteAllText(
                Path.Combine(directory, "b-valid.json"),
                JsonSerializer.Serialize(TestData.ValidExperiment()));

            bool success = ExperimentFileReader.TryReadDataset(directory, out var records, out var error);

            Assert.True(success);
            Assert.Null(error);
            Assert.Equal(2, records.Count);
            Assert.Equal(new[] { "a-broken.json", "b-valid.json" }, records.Select(r => r.SourceFile));
            Assert.Null(records[0].Input);
            Assert.Contains("invalid JSON", records[0].ReadError, StringComparison.OrdinalIgnoreCase);
            Assert.NotNull(records[1].Input);
            Assert.Null(records[1].ReadError);
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
