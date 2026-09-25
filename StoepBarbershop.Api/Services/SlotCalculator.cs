namespace StoepBarbershop.Api.Services;

public static class SlotCalculator
{
    // The grid of candidate start times for a given day + service duration,
    // e.g. open=510, close=1080, duration=40 -> 510, 525, 540, ... while the
    // service still fits before closing. Mirrors booking.js getAvailableSlots.
    public static List<int> GenerateGridStarts(int open, int close, int durationMinutes)
    {
        var starts = new List<int>();
        for (var start = open; start + durationMinutes <= close; start += ShopSchedule.SlotIntervalMinutes)
        {
            starts.Add(start);
        }
        return starts;
    }

    // The set of 15-minute slot rows a booking of [start, start+duration) occupies.
    // Always aligned to the same grid as GenerateGridStarts, so a booking's
    // slot rows exactly match slots other bookings could have chosen.
    public static List<int> SlotsFor(int startMinutes, int durationMinutes)
    {
        var end = startMinutes + durationMinutes;
        var slots = new List<int>();
        for (var s = startMinutes; s < end; s += ShopSchedule.SlotIntervalMinutes)
        {
            slots.Add(s);
        }
        return slots;
    }
}
