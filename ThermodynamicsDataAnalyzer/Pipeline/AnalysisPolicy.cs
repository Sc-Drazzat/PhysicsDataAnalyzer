using PhysicsDataAnalyzer.Models;
using PhysicsDataAnalyzer.Physics;

namespace PhysicsDataAnalyzer.Pipeline;

public class AnalysisPolicy
{
    public delegate bool SpecificHeatPolicy(ValidatedExperiment experiment, out double estimate);
    
    public static bool TryEstimateWholeExperimentPolicy(ValidatedExperiment experiment, out double estimate)
    {
        estimate = 0;
        var elapsedTimeSeconds = ThermodynamicsCalculator.CalculateElapsedTimeSeconds(experiment.Measurements);
        var temperatureChangeCelsius = ThermodynamicsCalculator.CalculateTemperatureChangeCelsius(experiment.Measurements);
        var energySuppliedJoules = ThermodynamicsCalculator.CalculateEnergySupplied(experiment.HeaterPowerW, elapsedTimeSeconds);

        return SpecificHeatEstimator.TryEstimateWholeExperiment(energySuppliedJoules, experiment.MassKg,
            temperatureChangeCelsius, out estimate);
    }

    public static bool TryEstimateMeanIntervalPolicy(ValidatedExperiment experiment, out double estimate)
    {
        estimate = 0;
        var intervals = IntervalCalculator.CalculateIntervals(experiment);

        return SpecificHeatEstimator.TryEstimateMeanInterval(intervals, experiment.MassKg, out estimate);
    }
}