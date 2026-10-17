namespace PhysicsDataAnalyzer.Models;

/// <summary>Result of analyzing one experiment. Success contains its report;
/// Failure contains reasons why no report could be produced.</summary>
public abstract record AnalysisOutcome
{
    private AnalysisOutcome() { }

    public sealed record Success : AnalysisOutcome
    {
        public ExperimentReport Report { get; }

        public Success(ExperimentReport report) =>
            Report = report ?? throw new ArgumentNullException(nameof(report));
    }

    public sealed record Failure : AnalysisOutcome
    {
        public IReadOnlyList<string> Reasons { get; }

        public Failure(IReadOnlyList<string> reasons)
        {
            ArgumentNullException.ThrowIfNull(reasons);
            if (reasons.Count == 0)
                throw new ArgumentException("At least one failure reason is required.", nameof(reasons));

            Reasons = Array.AsReadOnly(reasons.ToArray());
        }
    }
}
