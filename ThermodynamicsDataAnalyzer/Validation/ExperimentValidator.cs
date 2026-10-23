using PhysicsDataAnalyzer.Models;

namespace PhysicsDataAnalyzer.Validation;

public static class ExperimentValidator
{
    public static bool TryValidate(
        ExperimentInput? input,
        out ValidatedExperiment? experiment,
        out string? errorMessage)
    {
        switch (ExperimentFactory.Create(input))
        {
            case Result<ValidatedExperiment, DomainError>.Success valid:
                experiment = valid.Value;
                errorMessage = null;
                return true;

            case Result<ValidatedExperiment, DomainError>.Failure failure:
                experiment = null;
                errorMessage = failure.Error.Message;
                return false;

            default:
                throw new InvalidOperationException("Unknown validation result");
        }
    }
}
