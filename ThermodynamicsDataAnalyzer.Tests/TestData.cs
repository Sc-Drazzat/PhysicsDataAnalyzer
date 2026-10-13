using PhysicsDataAnalyzer.Models;

namespace PhysicsDataAnalyzer.Tests;

internal static class TestData
{
    public static ExperimentInput ValidExperiment(
        IReadOnlyList<TemperatureMeasurement>? measurements = null,
        double massKg = 0.2) => new(
        "test-001",
        "water",
        massKg,
        100,
        4180,
        measurements ?? new[]
        {
            new TemperatureMeasurement(0, 20),
            new TemperatureMeasurement(20, 22),
            new TemperatureMeasurement(40, 24)
        });

    public static ValidatedExperiment Validated(
        IReadOnlyList<TemperatureMeasurement>? measurements = null,
        double massKg = 0.2)
    {
        Assert.True(PhysicsDataAnalyzer.Validation.ExperimentValidator.TryValidate(
            ValidExperiment(measurements, massKg),
            out var experiment,
            out _));
        return experiment!;
    }
}
