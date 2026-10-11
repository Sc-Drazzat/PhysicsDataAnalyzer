namespace PhysicsDataAnalyzer.Models;

public sealed record ValidatedExperiment
{
    public string ExperimentId { get; }
    public string Material { get; }
    public double MassKg { get; }
    public double HeaterPowerW { get; }
    public double HeatCapacityJPerKgK { get; }
    public IReadOnlyList<TemperatureMeasurement> Measurements { get; }

    internal ValidatedExperiment(ExperimentInput input)
    {
        ExperimentId = input.ExperimentId;
        Material = input.Material;
        MassKg = input.MassKg;
        HeaterPowerW = input.HeaterPowerW;
        HeatCapacityJPerKgK = input.HeatCapacityJPerKgK;
        Measurements = input.Measurements.ToList().AsReadOnly();
    }
}