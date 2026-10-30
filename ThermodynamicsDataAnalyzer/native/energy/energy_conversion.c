double pda_joules_to_watt_hours(double joules)
{
    if (joules < 0.0) {
        return -1;
    }

    return joules / 3600.0;
}
