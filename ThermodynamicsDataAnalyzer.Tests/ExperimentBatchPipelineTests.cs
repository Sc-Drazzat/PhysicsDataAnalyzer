using PhysicsDataAnalyzer.Models;
using PhysicsDataAnalyzer.Pipeline;
using AnalysisPolicyType = PhysicsDataAnalyzer.Models.AnalysisPolicy;

namespace PhysicsDataAnalyzer.Tests;

public class ExperimentBatchPipelineTests
{
    [Fact]
    public void AnalyzesAllValidRecordsAndKeepsTheSelectedPolicyInReport()
    {
        var records = new[]
        {
            new ExperimentFileRecord("one.json", UnevenExperiment("one"), null),
            new ExperimentFileRecord("two.json", TestData.ValidExperiment(), null)
        };

        bool success = ExperimentPipeline.TryAnalyzeBatch(
            records,
            AnalysisPolicyType.WholeExperiment,
            out var error,
            out var report);

        Assert.True(success);
        Assert.Null(error);
        Assert.NotNull(report);
        Assert.Equal(2, report.TotalRecords);
        Assert.Equal(2, report.Accepted.Count);
        Assert.Empty(report.Rejected);
        Assert.Equal(AnalysisPolicyType.WholeExperiment, report.Policy);
        Assert.Equal("one.json", report.Accepted[0].SourceFile);
    }

    [Fact]
    public void RejectsBadRecordsAndContinuesAnalyzingTheBatch()
    {
        var records = new[]
        {
            new ExperimentFileRecord("valid.json", TestData.ValidExperiment(), null),
            new ExperimentFileRecord("invalid.json", TestData.ValidExperiment(massKg: 0), null),
            new ExperimentFileRecord("broken.json", null, "Invalid JSON")
        };

        bool success = ExperimentPipeline.TryAnalyzeBatch(
            records,
            AnalysisPolicyType.WholeExperiment,
            out var error,
            out var report);

        Assert.True(success);
        Assert.Null(error);
        Assert.NotNull(report);
        Assert.Equal(3, report.TotalRecords);
        Assert.Single(report.Accepted);
        Assert.Equal(2, report.Rejected.Count);
        Assert.Equal(new[] { "invalid.json", "broken.json" },
            report.Rejected.Select(item => item.SourceFile));
    }

    [Fact]
    public void EmptyDatasetProducesAnEmptyBatchReport()
    {
        bool success = ExperimentPipeline.TryAnalyzeBatch(
            Array.Empty<ExperimentFileRecord>(),
            AnalysisPolicyType.WholeExperiment,
            out var error,
            out var report);

        Assert.True(success);
        Assert.Null(error);
        Assert.NotNull(report);
        Assert.Equal(0, report.TotalRecords);
        Assert.Empty(report.Accepted);
        Assert.Empty(report.Rejected);
    }

    [Fact]
    public void RequiresPolicyBeforeProcessingRecords()
    {
        bool success = ExperimentPipeline.TryAnalyzeBatch(
            Array.Empty<ExperimentFileRecord>(),
            (AnalysisPolicyType)999,
            out var error,
            out var report);

        Assert.False(success);
        Assert.Contains("policy", error, StringComparison.OrdinalIgnoreCase);
        Assert.Null(report);
    }

    [Fact]
    public void AppliesDifferentPoliciesToTheSameUnevenInput()
    {
        var sameRecords = new[]
        {
            new ExperimentFileRecord("uneven.json", UnevenExperiment("uneven"), null)
        };

        Assert.True(ExperimentPipeline.TryAnalyzeBatch(
            sameRecords,
            AnalysisPolicyType.WholeExperiment,
            out _,
            out var wholeReport));
        Assert.True(ExperimentPipeline.TryAnalyzeBatch(
            sameRecords,
            AnalysisPolicyType.MeanInterval,
            out _,
            out var meanReport));

        Assert.NotNull(wholeReport);
        Assert.NotNull(meanReport);
        double wholeEstimate = wholeReport.Accepted[0].Report.Result.EstimatedSpecificHeatCapacityJPerKgK;
        double meanEstimate = meanReport.Accepted[0].Report.Result.EstimatedSpecificHeatCapacityJPerKgK;
        Assert.Equal(4545.454545, wholeEstimate, 4);
        Assert.Equal(5333.333333, meanEstimate, 5);
        Assert.NotEqual(wholeEstimate, meanEstimate);
    }

    [Fact]
    public void PreservesInputMeasurementsWhenAnalyzingBatch()
    {
        var inputMeasurements = new List<TemperatureMeasurement?>
        {
            new(0, 20), new(30, 25)
        };
        var record = new ExperimentFileRecord(
            "snapshot.json",
            TestData.ValidExperiment() with { Measurements = inputMeasurements },
            null);

        Assert.True(ExperimentPipeline.TryAnalyzeBatch(
            new[] { record },
            AnalysisPolicyType.WholeExperiment,
            out _,
            out var report));

        inputMeasurements[0] = new TemperatureMeasurement(0, -100);
        Assert.NotNull(report);
        Assert.Equal(20, report.Accepted[0].Report.InitialTemperatureCelsius);
    }

    private static ExperimentInput UnevenExperiment(string id) =>
        TestData.ValidExperiment(new[]
        {
            new TemperatureMeasurement(0, 20),
            new TemperatureMeasurement(20, 21),
            new TemperatureMeasurement(40, 24),
            new TemperatureMeasurement(60, 27),
            new TemperatureMeasurement(80, 29),
            new TemperatureMeasurement(100, 31)
        }) with { ExperimentId = id };
}
