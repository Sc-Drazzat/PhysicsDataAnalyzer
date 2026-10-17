using PhysicsDataAnalyzer.Models;

namespace PhysicsDataAnalyzer.Validation;

/// <summary>Compatibility adapter for callers that still use the Try pattern.</summary>
public static class ExperimentValidator
{
    public static bool TryValidate(
        ExperimentInput? input,
        out ValidatedExperiment? experiment,
        out string? errorMessage)
    {
        switch (ExperimentFactory.Create(input))
        {
            case ValidationOutcome.Valid valid:
                experiment = valid.Value;
                errorMessage = null;
                return true;

            case ValidationOutcome.Invalid invalid:
                experiment = null;
                errorMessage = string.Join(" ", invalid.Reasons);
                return false;

            default:
                throw new InvalidOperationException("Unknown validation outcome");
        }
    }
}
