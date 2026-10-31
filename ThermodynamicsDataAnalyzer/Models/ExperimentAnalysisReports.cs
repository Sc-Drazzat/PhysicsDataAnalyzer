namespace PhysicsDataAnalyzer.Models;

using PhysicsDataAnalyzer.Pipeline;

public sealed record ExperimentAnalysisReports(
    ExperimentBatchReport BatchReport,
    IReadOnlyList<EnergyConversionReport> EnergyConversions,
    IReadOnlyList<ExperimentAccuracyRow> AccuracyRanking,
    IReadOnlyList<MaterialSummaryRow> MaterialSummaries,
    IReadOnlyList<ScoredExperimentRow> LenientScores,
    IReadOnlyList<ScoredExperimentRow> StrictScores);
