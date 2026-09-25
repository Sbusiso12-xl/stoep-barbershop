using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StoepBarbershop.Api.Models;

namespace StoepBarbershop.Api.Data;

// Seeds the catalog (services/barbers) straight from the values currently
// hard-coded in assets/js/data.js, plus one login per barber for the
// dashboard. Safe to run every startup — it only inserts what's missing.
public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        // Applies any migrations that haven't been run yet — this is what
        // `dotnet ef migrations add` files get used for.
        await db.Database.MigrateAsync();

        if (!await db.Services.AnyAsync())
        {
            db.Services.AddRange(
                new Service { Id = "classic", Name = "Classic Cut", Price = 220, DurationMinutes = 40, Description = "A clean, considered cut with clippers and shears, finished with a straight-razor neckline." },
                new Service { Id = "fade", Name = "Skin Fade", Price = 260, DurationMinutes = 45, Description = "Precision fade blended down to the skin, shaped around your hairline and built up on top to suit you." },
                new Service { Id = "beard", Name = "Beard Trim & Line-up", Price = 150, DurationMinutes = 25, Description = "Beard shaped, edges sharpened, hot towel finish." },
                new Service { Id = "combo", Name = "Cut + Beard Combo", Price = 350, DurationMinutes = 60, Description = "The Classic Cut or Skin Fade paired with a full beard trim and line-up, done in one sitting." },
                new Service { Id = "kids", Name = "Kids Cut", Price = 150, DurationMinutes = 30, Description = "For customers 12 and under. Same care, shorter chair time, no fuss." },
                new Service { Id = "full", Name = "The Full Stoep", Price = 480, DurationMinutes = 75, Description = "Cut, beard, hot towel and a scalp massage." }
            );
        }

        if (!await db.Barbers.AnyAsync())
        {
            db.Barbers.AddRange(
                new Barber { Id = "neo", Name = "Neo Mahlangu", Role = "Owner & Senior Barber", Specialty = "Skin fades and precision line-ups", Years = 9, Bio = "Neo opened Stoep in 2016." },
                new Barber { Id = "junior", Name = "Junior Sithole", Role = "Barber", Specialty = "Beard sculpting", Years = 5, Bio = "Junior joined Stoep in 2021." },
                new Barber { Id = "amahle", Name = "Amahle Dube", Role = "Barber", Specialty = "Classic and textured cuts", Years = 4, Bio = "Amahle trained in Johannesburg before moving to Pretoria in 2022." }
            );
            await db.SaveChangesAsync();
        }

        if (!await db.BarberAccounts.AnyAsync())
        {
            var hasher = new PasswordHasher<object>();
            // NOTE: these are placeholder demo passwords — rotate them before
            // going anywhere near production, e.g. via a one-time setup script.
            db.BarberAccounts.AddRange(
                new BarberAccount { BarberId = "neo", Username = "neo", IsOwner = true, PasswordHash = hasher.HashPassword(null!, "ChangeMe123!") },
                new BarberAccount { BarberId = "junior", Username = "junior", IsOwner = false, PasswordHash = hasher.HashPassword(null!, "ChangeMe123!") },
                new BarberAccount { BarberId = "amahle", Username = "amahle", IsOwner = false, PasswordHash = hasher.HashPassword(null!, "ChangeMe123!") }
            );
        }

        await db.SaveChangesAsync();
    }
}
