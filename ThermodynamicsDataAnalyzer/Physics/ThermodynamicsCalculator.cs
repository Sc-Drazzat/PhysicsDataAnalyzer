using System.Collections.Frozen;
using PhysicsDataAnalyzer.Models;

namespace PhysicsDataAnalyzer.Physics;

public class ThermodynamicsCalculator
{
    public static double CalculateElapsedTimeSeconds(IReadOnlyList<TemperatureMeasurement> measurements)
    {
        double initialTime = measurements.First().TimeSeconds;
        double finalTime = measurements.Last().TimeSeconds;

        return finalTime - initialTime;
    }

    public static double CalculateTemperatureChangeCelsius(IReadOnlyList<TemperatureMeasurement> measurements)
    {
        double initialTemperature = measurements.First().TemperatureCelsius;
        double finalTemperature = measurements.Last().TemperatureCelsius;
        
        return finalTemperature - initialTemperature;
    }

    public static double CalculateEnergySupplied(double powerWatts, double elapsedTimeSeconds)
    {
        return powerWatts * elapsedTimeSeconds;
    }
    
    public static double CalculateIntervalEnergySupplied(double powerWatts, double IntervalDurationSeconds)
    {
        return powerWatts * IntervalDurationSeconds;
    }
}