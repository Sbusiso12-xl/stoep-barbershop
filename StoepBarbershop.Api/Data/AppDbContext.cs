using Microsoft.EntityFrameworkCore;
using StoepBarbershop.Api.Models;

namespace StoepBarbershop.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Service> Services => Set<Service>();
    public DbSet<Barber> Barbers => Set<Barber>();
    public DbSet<BarberAccount> BarberAccounts => Set<BarberAccount>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookedSlot> BookedSlots => Set<BookedSlot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Service>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.Id).HasMaxLength(40);
        });

        modelBuilder.Entity<Barber>(e =>
        {
            e.HasKey(b => b.Id);
            e.Property(b => b.Id).HasMaxLength(40);
        });

        modelBuilder.Entity<BarberAccount>(e =>
        {
            e.HasIndex(a => a.Username).IsUnique();
            e.HasOne(a => a.Barber)
                .WithOne(b => b.Account)
                .HasForeignKey<BarberAccount>(a => a.BarberId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Booking>(e =>
        {
            e.HasIndex(b => b.Reference).IsUnique();

            e.HasOne(b => b.Service)
                .WithMany()
                .HasForeignKey(b => b.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(b => b.Barber)
                .WithMany(barber => barber.Bookings)
                .HasForeignKey(b => b.BarberId)
                .OnDelete(DeleteBehavior.Restrict);

            // Fast lookups for the dashboard (by barber + date range).
            e.HasIndex(b => new { b.BarberId, b.Date });
        });

        modelBuilder.Entity<BookedSlot>(e =>
        {
            // THE constraint that prevents clashing/double-bookings: the database
            // itself refuses a second slot row for the same barber/date/start,
            // regardless of how many requests race to insert it concurrently.
            e.HasIndex(s => new { s.BarberId, s.Date, s.SlotStart }).IsUnique();

            e.HasOne(s => s.Booking)
                .WithMany(b => b.Slots)
                .HasForeignKey(s => s.BookingId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
