namespace PhysicsDataAnalyzer.Models;

public enum AnalysisPolicy
{
    WholeExperiment,
    MeanInterval
}
public record AnalysisResult(
    AnalysisPolicy Policy,
    double EstimatedSpecificHeatCapacityJPerKgK,
    double SpecificHeatCapacityJPerKgK,
    double AbsoluteErrorJPerKgK, 
    double PercentageError 
    );