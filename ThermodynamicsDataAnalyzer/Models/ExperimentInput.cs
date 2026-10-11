namespace PhysicsDataAnalyzer.Models;

public record ExperimentInput(
    string ExperimentId,
    string Material,
    double MassKg,
    double HeaterPowerW,
    double TheoreticalSpecificHeatJPerKgk,
    List<TemperatureMeasurement> Measurements
    );