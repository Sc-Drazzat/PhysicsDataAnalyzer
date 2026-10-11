using System.Collections.Frozen;
using PhysicsDataAnalyzer.Models;

namespace PhysicsDataAnalyzer.Physics;

public class ThermodynamicsCalculator
{
    public static double CalculateElapsedTime(IReadOnlyList<TemperatureMeasurement> measurements)
    {
        double initialTime = measurements.First().TimeSeconds;
        double finalTime = measurements.Last().TimeSeconds;

        return finalTime - initialTime;
    }
}