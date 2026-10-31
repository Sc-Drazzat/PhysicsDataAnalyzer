using PhysicsDataAnalyzer.Models;
using PhysicsDataAnalyzer.Pipeline;
using PhysicsDataAnalyzer.Reports;

namespace PhysicsDataAnalyzer.Tests;

public class ExperimentScoreTests
{
    [Fact]
    public void LenientAndStrictScoresApplyDifferentPenaltiesToSameReport()
    {
        var report = new ExperimentReport("test", "Water", 20, 25, 5, 10, 100,
            new AnalysisResult(PhysicsDataAnalyzer.Models.AnalysisPolicy.WholeExperiment, 120, 100, 20, 20));
        Func<ExperimentReport, double> lenientScore = ExperimentScore.CreateScore(ScoreConfiguration.Lenient);
        Func<ExperimentReport, double> strictScore = ExperimentScore.CreateScore(ScoreConfiguration.Strict);

        Assert.Equal(90, lenientScore(report));
        Assert.Equal(60, strictScore(report));

        var scoredReports = ExperimentReportQueries.BuildScoredReports(new[] { report }, strictScore);
        Assert.Equal(new ScoredExperimentRow(report, 60), Assert.Single(scoredReports));
    }
}
