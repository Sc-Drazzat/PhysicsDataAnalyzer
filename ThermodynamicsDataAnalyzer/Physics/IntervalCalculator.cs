using PhysicsDataAnalyzer.Models;

namespace PhysicsDataAnalyzer.Physics;

public class IntervalCalculator
{
    public static List<CalculatedInterval> CalculateIntervals(ValidatedExperiment experiment)
    {
        var measurements = experiment.Measurements;
        var intervals = new List<CalculatedInterval>();

        for (int i = 1; i < measurements.Count; i++)
        {
            var previous = measurements[i-1];
            var current = measurements[i];
            
            double durationSeconds = current.TimeSeconds - previous.TimeSeconds;
            double energyJoules = experiment.HeaterPowerW * durationSeconds;
            
            intervals.Add(new CalculatedInterval(previous.TimeSeconds, current.TimeSeconds, previous.TemperatureCelsius, current.TemperatureCelsius, energyJoules));
        }
        return intervals;
    }
}