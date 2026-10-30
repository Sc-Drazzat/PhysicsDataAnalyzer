using PhysicsDataAnalyzer.Extensions;
using PhysicsDataAnalyzer.Models;

namespace PhysicsDataAnalyzer.Tests;

public class ExperimentReportExtensionsTests
{
    [Fact]
    public void WhereReportableFiltersNonFiniteEstimatesAndInvalidReferences()
    {
        var reports = new[]
        {
            Report("valid", 120, 100),
            Report("bad-estimate", double.NaN, 100),
            Report("bad-reference", 120, double.PositiveInfinity),
            Report("zero-reference", 120, 0)
        };

        Assert.Equal(new[] { "valid" }, reports.WhereReportable().Select(report => report.ExperimentId));
    }

    [Fact]
    public void ToAccuracyRowsCalculatesErrorsAndProjectsReportFields()
    {
        var rows = new[] { Report("water-1", 120, 100) }.ToAccuracyRows().ToArray();

        Assert.Equal(new ExperimentAccuracyRow("water-1", "Water", AnalysisPolicy.MeanInterval,
            120, 100, 20, 20), Assert.Single(rows));
    }

    [Fact]
    public void InvalidValuesRemainInvalidWhenProjected()
    {
        var rows = new[] { Report("invalid", double.PositiveInfinity, 0) }.ToAccuracyRows().Single();

        Assert.True(double.IsPositiveInfinity(rows.EstimatedHeatCapacity));
        Assert.Equal(0, rows.ReferenceHeatCapacity);
        Assert.True(double.IsPositiveInfinity(rows.AbsoluteError));
        Assert.True(double.IsPositiveInfinity(rows.PercentageError));
    }

    private static ExperimentReport Report(string id, double estimate, double reference) =>
        new(id, "Water", 20, 25, 5, 10, 100,
            new AnalysisResult(AnalysisPolicy.MeanInterval, estimate, reference, 999, 999));
}
