namespace StoepBarbershop.Api.Models;

// Each row represents one 15-minute slot, for one barber, on one date, being
// occupied by a booking. A unique constraint on (BarberId, Date, SlotStart)
// (configured in AppDbContext) is the mechanism that guarantees two
// overlapping bookings for the same barber can never both commit, even
// under concurrent requests — the database rejects the second insert.
public class BookedSlot
{
    public int Id { get; set; }

    public Guid BookingId { get; set; }
    public Booking Booking { get; set; } = default!;

    public string BarberId { get; set; } = default!;
    public string Date { get; set; } = default!;
    public int SlotStart { get; set; }
}
