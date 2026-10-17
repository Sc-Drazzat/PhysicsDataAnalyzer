namespace PhysicsDataAnalyzer.Models;

public abstract record ValidationOutcome
{
    private ValidationOutcome(){}

    public sealed record Valid(ValidatedExperiment Value) : ValidationOutcome;

    public sealed record Invalid(IReadOnlyList<string> Reasons) : ValidationOutcome;
}