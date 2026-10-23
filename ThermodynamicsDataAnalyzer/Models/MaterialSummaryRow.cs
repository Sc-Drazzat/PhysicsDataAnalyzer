namespace PhysicsDataAnalyzer.Models;

public sealed record MaterialSummaryRow(
    string Material,
    int ExperimentCount,
    double MeanEstimatedHeatCapacity,
    double TotalEnergyJoules,
    double MeanPercentageError);
