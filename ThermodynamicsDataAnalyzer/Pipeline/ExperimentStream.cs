using PhysicsDataAnalyzer.Models;

namespace PhysicsDataAnalyzer.Pipeline;

public interface INativeEnergyConverter
{
    double ConvertJoules(double energyJoules);
}

public sealed record EnergyConversionResult(
    string ExperimentId,
    double EnergyJoules,
    double ConvertedEnergy);

public static class ExperimentStream
{
    public static IEnumerable<EnergyConversionResult> EnumerateEnergyConversions(
        IEnumerable<ExperimentReport> reports,
        INativeEnergyConverter converter)
    {
        ArgumentNullException.ThrowIfNull(reports);
        ArgumentNullException.ThrowIfNull(converter);

        foreach (ExperimentReport report in reports)
        {
            double energyJoules = report.TotalEnergySuppliedJoules;
            yield return new EnergyConversionResult(
                report.ExperimentId,
                energyJoules,
                converter.ConvertJoules(energyJoules));
        }
    }
}
