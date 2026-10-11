using PhysicsDataAnalyzer.Models;

namespace PhysicsDataAnalyzer.Physics;

public static class SpecificHeatEstimator
{
    private static bool TryCalculateSpecificHeatJperKg (double energyJoules, double massKg, double temperatureChangeKelvin, out double specificHeatJPerKg)
    {
        specificHeatJPerKg = 0;

        if (energyJoules < 0)
            return false;

        if (massKg <= 0)
            return false;

        if (temperatureChangeKelvin <= 0)
            return false;

        specificHeatJPerKg = energyJoules / (massKg * temperatureChangeKelvin);

        return double.IsFinite(specificHeatJPerKg);
    }

    public static bool TryEstimateWholeExperiment(double energyJoules, double massKg, double temperatureChangeKelvin, out double estimatedSpecificHeatCapacityJPerKg)
    {
        return TryCalculateSpecificHeatJperKg(energyJoules, massKg, temperatureChangeKelvin, out estimatedSpecificHeatCapacityJPerKg);
    }

    public static bool TryEstimateMeanInterval(IReadOnlyList<CalculatedInterval> intervals, double massKg, out double estimatedSpecificHeatCapacityJPerKg)
    {
        estimatedSpecificHeatCapacityJPerKg = 0;

        if (intervals == null || intervals.Count == 0)
            return false;

        double total = 0;

        foreach (var interval in intervals)
        {
            if (!TryCalculateSpecificHeatJperKg(interval.SuppliedEnergyJoules, massKg, interval.TemperatureChangeCelsius, out double intervalEstimate))
            {
                return false;
            }

            total += intervalEstimate;

            if (!double.IsFinite(total))
                return false;
        }

        estimatedSpecificHeatCapacityJPerKg = total / intervals.Count;
        return double.IsFinite(estimatedSpecificHeatCapacityJPerKg);
    }
}