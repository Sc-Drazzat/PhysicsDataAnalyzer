namespace PhysicsDataAnalyzer.Models;

public sealed record ExperimentAccuracyRow(
    string ExperimentId,
    string Material,
    AnalysisPolicy Policy,
    double EstimatedHeatCapacity,
    double ReferenceHeatCapacity,
    double AbsoluteError,
    double PercentageError);
