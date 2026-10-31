namespace PhysicsDataAnalyzer.Models;

public sealed record EnergyConversionReport(
    string ExperimentId,
    double EnergyJoules,
    double WattHours);
