using PhysicsDataAnalyzer.Models;

namespace PhysicsDataAnalyzer.Pipeline;

public sealed class ExperimentCatalog
{
    private readonly IReadOnlyList<ExperimentFileRecord> _experiments;

    public ExperimentCatalog(IEnumerable<ExperimentFileRecord> experiments)
    {
        ArgumentNullException.ThrowIfNull(experiments);
        _experiments = experiments.ToArray();
    }

    public Maybe<ExperimentFileRecord> FindById(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return new Maybe<ExperimentFileRecord>.None();

        foreach (var experiment in _experiments)
        {
            if (experiment.Input is { } input &&
                string.Equals(input.ExperimentId, id, StringComparison.Ordinal))
            {
                return new Maybe<ExperimentFileRecord>.Some(experiment);
            }
        }

        return new Maybe<ExperimentFileRecord>.None();
    }
}
