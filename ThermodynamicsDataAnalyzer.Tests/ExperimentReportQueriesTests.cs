using PhysicsDataAnalyzer.Models;
using PhysicsDataAnalyzer.Reports;

namespace PhysicsDataAnalyzer.Tests;

public class ExperimentReportQueriesTests
{
    [Fact]
    public void BuildsAccuracyRankingWithExpectedValuesAndDescendingError()
    {
        var reports = new[]
        {
            Report("b", "Water", 130, 100, 30, 20, 30),
            Report("a", "Iron", 80, 100, 20, 20, 1)
        };

        var rows = ExperimentReportQueries.BuildAccuracyRanking(reports);

        Assert.Equal(new[] { "b", "a" }, rows.Select(row => row.ExperimentId));
        Assert.Equal(new ExperimentAccuracyRow("b", "Water", AnalysisPolicy.MeanInterval, 130, 100, 30, 30), rows[0]);
    }

    [Fact]
    public void EmptyInputProducesEmptyQueries()
    {
        Assert.Empty(ExperimentReportQueries.BuildAccuracyRanking(Array.Empty<ExperimentReport>()));
        Assert.Empty(ExperimentReportQueries.BuildMaterialSummaries(Array.Empty<ExperimentReport>()));
    }

    [Fact]
    public void FiltersReportsWithoutReportableAccuracyValues()
    {
        var reports = new[] { Report("bad", "Water", 10, 0, 10, 100, 1) };

        Assert.Empty(ExperimentReportQueries.BuildAccuracyRanking(reports));
        Assert.Empty(ExperimentReportQueries.BuildMaterialSummaries(reports));
    }

    [Fact]
    public void BuildsMaterialGroupTotalsAndMeans()
    {
        var reports = new[]
        {
            Report("a", "Water", 120, 100, 20, 30, 3),
            Report("b", "Water", 80, 100, 20, 70, 7),
            Report("c", "Iron", 50, 100, 50, 110, 50)
        };

        var rows = ExperimentReportQueries.BuildMaterialSummaries(reports);

        Assert.Equal(new MaterialSummaryRow("Iron", 1, 50, 110, 50), rows[0]);
        Assert.Equal(new MaterialSummaryRow("Water", 2, 100, 100, 5), rows[1]);
    }

    [Fact]
    public void UsesOrdinalTieBreakersForBothQueries()
    {
        var reports = new[]
        {
            Report("z", "Zulu", 100, 100, 0, 10, 5),
            Report("a", "Alpha", 100, 100, 0, 10, 5)
        };

        Assert.Equal(new[] { "a", "z" }, ExperimentReportQueries.BuildAccuracyRanking(reports).Select(row => row.ExperimentId));
        Assert.Equal(new[] { "Alpha", "Zulu" }, ExperimentReportQueries.BuildMaterialSummaries(reports).Select(row => row.Material));
    }

    private static ExperimentReport Report(string id, string material, double estimate, double reference,
        double absoluteError, double energy, double percentageError) =>
        new(id, material, 20, 25, 5, 10, energy,
            new AnalysisResult(AnalysisPolicy.MeanInterval, estimate, reference, absoluteError, percentageError));
}
