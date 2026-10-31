
// TEACHER, I have macOS so .dll format is not supported in my device, and ig my .dylib doesn't work in yours too
#if defined(_WIN32)
#define PDA_EXPORT __declspec(dllexport)
#else
#define PDA_EXPORT
#endif

PDA_EXPORT double pda_joules_to_watt_hours(double joules)
{
    if (joules < 0.0) {
        return -1;
    }

    return joules / 3600.0;
}
