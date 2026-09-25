namespace StoepBarbershop.Api.Services;

// Mirrors assets/js/data.js SHOP.hoursByDay so the server is the single
// source of truth for opening hours (the front end can keep its own copy
// for instant rendering, but the server never trusts the client's idea of
// what's open).
public static class ShopSchedule
{
    public const int SlotIntervalMinutes = 15;
    public const int MinNoticeMinutes = 30;

    // Key: DayOfWeek. Value: (openMinutes, closeMinutes) or null if closed.
    private static readonly Dictionary<DayOfWeek, (int Open, int Close)?> HoursByDay = new()
    {
        [DayOfWeek.Sunday] = null,
        [DayOfWeek.Monday] = (510, 1080),    // 08:30–18:00
        [DayOfWeek.Tuesday] = (510, 1080),
        [DayOfWeek.Wednesday] = (510, 1080),
        [DayOfWeek.Thursday] = (510, 1080),
        [DayOfWeek.Friday] = (510, 1080),
        [DayOfWeek.Saturday] = (480, 900),   // 08:00–15:00
    };

    public static (int Open, int Close)? GetHours(DateOnly date) => HoursByDay[date.DayOfWeek];
}
