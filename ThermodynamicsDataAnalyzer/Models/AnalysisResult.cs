namespace PhysicsDataAnalyzer.Models;

public record AnalysisResult(
    string PolicyName,
    double EstimatedSpecificHeatCapacityJPerKgK,
    double SpecificHeatCapacityJPerKgK,
    double AbsoluteErrorJPerKgK, 
    double PercentageError 
    );