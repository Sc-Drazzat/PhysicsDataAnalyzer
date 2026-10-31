using PhysicsDataAnalyzer.Models;
using PhysicsDataAnalyzer.Pipeline;

namespace PhysicsDataAnalyzer.Tests;

public class ExperimentStreamTests
{
    [Fact]
    public void DefersConversionsUntilEnumeration()
    {
        var converter = new ProbeEnergyConverter();
        IEnumerable<EnergyConversionResult> conversions = ExperimentStream.EnumerateEnergyConversions(
            new[] { Report("one", 100) }, converter);

        Assert.Equal(0, converter.CallCount);

        Assert.Single(conversions);
        Assert.Equal(1, converter.CallCount);
    }

    [Fact]
    public void ReenumerationRepeatsWorkButMaterializedSnapshotCanBeReused()
    {
        var converter = new ProbeEnergyConverter();
        IEnumerable<EnergyConversionResult> conversions = ExperimentStream.EnumerateEnergyConversions(
            new[] { Report("one", 100), Report("two", 250) }, converter);

        EnergyConversionResult[] snapshot = conversions.ToArray();
        Assert.Equal(2, converter.CallCount);
        Assert.Equal(snapshot, conversions.ToArray());
        Assert.Equal(4, converter.CallCount);

        EnergyConversionResult[] firstReuse = snapshot.ToArray();
        EnergyConversionResult[] secondReuse = snapshot.ToArray();
        Assert.Equal(firstReuse, secondReuse);
        Assert.Equal(4, converter.CallCount);
    }

    private static ExperimentReport Report(string id, double energy) =>
        new(id, "Water", 20, 25, 5, 10, energy,
            new AnalysisResult(PhysicsDataAnalyzer.Models.AnalysisPolicy.WholeExperiment, 100, 100, 0, 0));

    private sealed class ProbeEnergyConverter : INativeEnergyConverter
    {
        public int CallCount { get; private set; }

        public Result<double, DomainError> ConvertJoules(double energyJoules)
        {
            CallCount++;
            return new Result<double, DomainError>.Success(energyJoules / 1000);
        }
    }
}
