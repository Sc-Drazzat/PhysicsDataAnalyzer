namespace PhysicsDataAnalyzer.Models;

public abstract record ValidationOutcome
{
    private ValidationOutcome() { }

    public sealed record Valid : ValidationOutcome
    {
        public ValidatedExperiment Value { get; }

        public Valid(ValidatedExperiment value) =>
            Value = value ?? throw new ArgumentNullException(nameof(value));
    }

    public sealed record Invalid : ValidationOutcome
    {
        public IReadOnlyList<string> Reasons { get; }

        public Invalid(IReadOnlyList<string> reasons)
        {
            ArgumentNullException.ThrowIfNull(reasons);
            if (reasons.Count == 0)
                throw new ArgumentException("At least one reason is required.", nameof(reasons));

            Reasons = Array.AsReadOnly(reasons.ToArray());
        }
    }
}
