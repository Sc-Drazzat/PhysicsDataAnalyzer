namespace PhysicsDataAnalyzer.Models;

public abstract record AnalysisOutcome
{
    private AnalysisOutcome() { }

    public sealed record Success(ExperimentReport Report) : AnalysisOutcome;
    public sealed record Failure(IReadOnlyList<string> Reasons) : AnalysisOutcome;
}
