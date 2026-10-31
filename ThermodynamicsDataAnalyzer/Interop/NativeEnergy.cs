using System.Runtime.InteropServices;

namespace PhysicsDataAnalyzer.Interop;

internal static class NativeEnergy
{
    [DllImport("pda_energy", EntryPoint = "pda_joules_to_watt_hours", ExactSpelling = true,
        CallingConvention = CallingConvention.Cdecl)]
    internal static extern double JoulesToWattHours(double joules);
}
