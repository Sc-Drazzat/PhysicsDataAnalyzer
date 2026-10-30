using PhysicsDataAnalyzer.Models;

namespace PhysicsDataAnalyzer.Pipeline;

public sealed record ScoreConfiguration(double PenaltyPerErrorPercent)
{
    public static ScoreConfiguration Lenient { get; } = new(0.5);
    public static ScoreConfiguration Strict { get; } = new(2.0);
}

public static class ExperimentScore
{
    public static Func<ExperimentReport, double> CreateScore(ScoreConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        return report => Math.Max(0, 100 - report.Result.PercentageError * configuration.PenaltyPerErrorPercent);
    }
}
