using PhysicsDataAnalyzer.Models;

namespace PhysicsDataAnalyzer.Physics;

public static class SpecificHeatEstimator
{
    private static bool TryCalculateSpecificHeat (double energyJoules, double massKg, double temperatureChangeKelvin, out double specificHeatJPerKg)
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

    
}