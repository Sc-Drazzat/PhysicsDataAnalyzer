namespace PhysicsDataAnalyzer.Models;

public sealed record ExperimentReport(
    string ExperimentId,
    string Material,
    double InitialTemperatureCelsius,
    double FinalTemperatureCelsius,
    double TemperatureChangeCelsius,
    double ElapsedTimeSeconds,
    double TotalEnergySuppliedJoules,
    AnalysisResult WholeExperimentResult,
    AnalysisResult MeanIntervalResult);
