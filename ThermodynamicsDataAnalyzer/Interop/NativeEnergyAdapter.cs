using PhysicsDataAnalyzer.Models;
using PhysicsDataAnalyzer.Pipeline;

namespace PhysicsDataAnalyzer.Interop;

public sealed class NativeEnergyAdapter : INativeEnergyConverter
{
    private readonly Func<double, double> _convert;

    public NativeEnergyAdapter(Func<double, double>? convert = null)
    {
        _convert = convert ?? NativeEnergy.JoulesToWattHours;
    }

    public Result<double, DomainError> ConvertJoules(double energyJoules)
    {
        if (!double.IsFinite(energyJoules) || energyJoules < 0)
            return new Result<double, DomainError>.Failure(
                new DomainError(DomainError.Codes.NativeEnergyInputInvalid,
                    "Energy must be finite and nonnegative."));

        try
        {
            double converted = _convert(energyJoules);
            if (!double.IsFinite(converted) || converted < 0)
                return new Result<double, DomainError>.Failure(
                    new DomainError(DomainError.Codes.NativeEntryPointUnavailable,
                        "Native energy conversion returned an invalid value."));

            return new Result<double, DomainError>.Success(converted);
        }
        catch (Exception exception) when (exception is DllNotFoundException or
                                          EntryPointNotFoundException or
                                          BadImageFormatException)
        {
            string code = exception is EntryPointNotFoundException
                ? DomainError.Codes.NativeEntryPointUnavailable
                : DomainError.Codes.NativeLibraryUnavailable;
            return Failure(code, exception.Message);
        }
    }

    private static Result<double, DomainError> Failure(string code, string message) =>
        new Result<double, DomainError>.Failure(new DomainError(code, message));
}
