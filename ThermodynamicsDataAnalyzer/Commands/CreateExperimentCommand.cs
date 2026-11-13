using System.Globalization;
using PhysicsDataAnalyzer.Models;
using PhysicsDataAnalyzer.Pipeline;

namespace PhysicsDataAnalyzer.Commands;

public static class CreateExperimentCommand
{
    public static int Run(string? outputPath)
    {
        Console.WriteLine("Create experiment (enter at least two measurements; leave time blank when finished).");
        string id = ReadText("Experiment ID: ");
        string material = ReadText("Material: ");
        double mass = ReadNumber("Mass (kg): ");
        double power = ReadNumber("Heater power (W): ");
        double reference = ReadNumber("Reference specific heat (J/(kg·K)): ");

        var measurements = new List<TemperatureMeasurement?>();
        while (true)
        {
            Console.Write("Measurement time (s; blank to finish): ");
            string? timeText = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(timeText))
                break;

            double time = ParseNumber(timeText, "measurement time");
            double temperature = ReadNumber("Temperature (°C): ");
            measurements.Add(new TemperatureMeasurement(time, temperature));
        }

        var input = new ExperimentInput(id, material, mass, power, reference, measurements);
        var result = ExperimentCreationWorkflow.Create(input, outputPath);
        switch (result)
        {
            case Result<string, DomainError>.Success success:
                Console.WriteLine($"Experiment saved to {success.Value}");
                return 0;
            case Result<string, DomainError>.Failure failure:
                Console.Error.WriteLine($"Creation error: {failure.Error.Message}");
                return 1;
            default:
                Console.Error.WriteLine("Creation error: Unknown result");
                return 1;
        }
    }

    private static string ReadText(string prompt)
    {
        Console.Write(prompt);
        return Console.ReadLine() ?? string.Empty;
    }

    private static double ReadNumber(string prompt) => ParseNumber(ReadText(prompt), prompt.TrimEnd(':', ' '));

    private static double ParseNumber(string value, string field)
    {
        if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double number))
            throw new FormatException($"{field} must be a number using '.' for decimals.");
        return number;
    }
}
