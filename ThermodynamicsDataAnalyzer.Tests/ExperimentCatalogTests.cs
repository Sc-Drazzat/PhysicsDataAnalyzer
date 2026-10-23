using PhysicsDataAnalyzer.Models;
using PhysicsDataAnalyzer.Pipeline;

namespace PhysicsDataAnalyzer.Tests;

public class ExperimentCatalogTests
{
    [Fact]
    public void FindsExperimentById()
    {
        var record = new ExperimentFileRecord("found.json", TestData.ValidExperiment(), null);
        var catalog = new ExperimentCatalog(new[] { record });

        var result = catalog.FindById("test-001");

        Assert.Same(record, Assert.IsType<Maybe<ExperimentFileRecord>.Some>(result).Value);
    }

    [Fact]
    public void ReturnsNoneForMissingId()
    {
        var catalog = new ExperimentCatalog(new[]
        {
            new ExperimentFileRecord("found.json", TestData.ValidExperiment(), null)
        });

        var result = catalog.FindById("missing");

        Assert.IsType<Maybe<ExperimentFileRecord>.None>(result);
    }

    [Fact]
    public void FindsAnInvalidRecordInsteadOfTreatingItAsMissing()
    {
        var record = new ExperimentFileRecord("invalid.json", TestData.ValidExperiment(massKg: 0), null);
        var catalog = new ExperimentCatalog(new[] { record });

        var result = catalog.FindById("test-001");

        Assert.Same(record, Assert.IsType<Maybe<ExperimentFileRecord>.Some>(result).Value);
    }

    [Fact]
    public void FindsRecordWhenMeasurementHasLegitimateZeroValue()
    {
        var input = TestData.ValidExperiment(new[]
        {
            new TemperatureMeasurement(0, 0),
            new TemperatureMeasurement(30, 2)
        });
        var record = new ExperimentFileRecord("zero.json", input, null);
        var catalog = new ExperimentCatalog(new[] { record });

        var result = catalog.FindById("test-001");

        var found = Assert.IsType<Maybe<ExperimentFileRecord>.Some>(result).Value;
        Assert.Same(record, found);
        var foundInput = Assert.IsType<ExperimentInput>(found.Input);
        var measurements = Assert.IsAssignableFrom<IReadOnlyList<TemperatureMeasurement?>>(foundInput.Measurements);
        Assert.Equal(0, Assert.IsType<TemperatureMeasurement>(measurements[0]).TemperatureCelsius);
    }
}
