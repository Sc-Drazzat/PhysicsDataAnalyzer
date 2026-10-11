namespace PhysicsDataAnalyzer.Models;

public enum AnalysisPolicy
{
    WholeExperiment,
    MeanInterval
}
public record AnalysisResult(
    AnalysisPolicy Policy,
    double EstimatedHeatCapacityJPerKgK,
    double HeatCapacityJPerKgK,
    double AbsoluteErrorJPerKgK, 
    double PercentageError 
    );