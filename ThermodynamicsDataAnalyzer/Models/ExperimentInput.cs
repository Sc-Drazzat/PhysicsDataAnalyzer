namespace PhysicsDataAnalyzer.Models;

public record ExperimentInput(
    string ExperimentId,
    string Material,
    double MassKg,
    double HeaterPowerW,
    double TheoreticalSpecificHeatJPerKgK,
    IReadOnlyList<TemperatureMeasurement> Measurements
    );