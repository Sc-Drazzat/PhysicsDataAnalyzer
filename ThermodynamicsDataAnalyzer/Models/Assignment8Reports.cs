namespace PhysicsDataAnalyzer.Models;

using PhysicsDataAnalyzer.Pipeline;

public sealed record Assignment8Reports(
    ExperimentBatchReport BatchReport,
    IReadOnlyList<EnergyConversionResult> EnergyConversions,
    IReadOnlyList<ExperimentAccuracyRow> AccuracyRanking,
    IReadOnlyList<MaterialSummaryRow> MaterialSummaries,
    IReadOnlyList<ScoredExperimentRow> LenientScores,
    IReadOnlyList<ScoredExperimentRow> StrictScores);
