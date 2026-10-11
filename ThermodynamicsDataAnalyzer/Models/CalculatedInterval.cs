namespace PhysicsDataAnalyzer.Models;

public sealed record CalculatedInterval(
    double StartTimeSeconds,
    double EndTimeSeconds,
    double StartTemperatureCelsius,
    double EndTemperatureCelsius,
    double SuppliedEnergyJoules)
{
    public double DurationSeconds => EndTimeSeconds - StartTimeSeconds;
    public double TemperatureChangeCelsius => EndTemperatureCelsius - StartTemperatureCelsius;
}