# Stoep Barbershop — Backend API

ASP.NET Core 8 Web API backing the booking wizard and a new barber dashboard.
Replaces the old `localStorage`-only booking flow with a real database and,
critically, a server-side guarantee that two customers can never be given the
same barber/time slot.

## Why the old flow could double-book

The original `booking.js` read/wrote bookings to `localStorage` in the
browser. That's per-device storage — customer A on their phone has no idea
what customer B just booked on their laptop. Two people could both see
"10:00 available" and both "successfully" book it. There was no shared
source of truth and no atomic check.

## How this backend prevents clashes

Two layers, from cheapest to strongest:

1. **In-process lock** (`BookingService`) — requests for the same
   `(barberId, date)` are serialized with a `SemaphoreSlim` so two requests
   hitting the same API instance at the same instant don't even race each
   other in application code.
2. **Database unique constraint** (the real guarantee) — every booking is
   decomposed into one `BookedSlot` row per 15-minute increment it occupies.
   `(BarberId, Date, SlotStart)` has a **unique index**. Creating a booking
   inserts its slot rows in the same transaction as the booking itself; if
   any of those slots is already taken, the insert violates the unique
   index and the whole transaction rolls back. This holds even if you scale
   out to multiple API instances behind a load balancer, because the
   guarantee lives in the database, not in memory.

`POST /api/bookings` returns **409 Conflict** (not a generic error) when this
happens, so the front end can tell the customer their slot just got taken
and send them back to pick another time — see the updated `booking.js`.

## Project layout

```
Models/        EF Core entities (Service, Barber, Booking, BookedSlot, BarberAccount)
Data/          AppDbContext (schema + the unique index) and DbSeeder (seeds the catalog)
Dtos/          Request/response shapes exposed by the API
Services/      BookingService (the concurrency-safe core), SlotCalculator, ShopSchedule, JwtTokenService
Controllers/   Services, Barbers, Availability, Bookings, Auth, Dashboard
```

## Running it

Requires the .NET 8 SDK.

```bash
cd StoepBarbershop.Api
dotnet restore
dotnet run
```

On first run it creates `stoep.db` (SQLite, in the project folder) and seeds
services, barbers, and one login per barber/owner — see `Data/DbSeeder.cs`
for the seeded usernames (default password `ChangeMe123!` for all three;
**change these before this goes anywhere near production**).

Swagger UI opens automatically at `https://localhost:.../swagger` in
Development mode — use it to try requests, including pasting a JWT into the
"Authorize" button for the dashboard endpoints.

Before deploying anywhere real:
- Set `Jwt:Key` (in `appsettings.json` or, better, an environment variable /
  secret manager) to a long random value — the checked-in one is a
  placeholder.
- Update `AllowedOrigins` to your actual front-end domain(s).
- Swap the seeded demo passwords for real ones.
- Consider moving from SQLite to SQL Server/PostgreSQL for production
  traffic (change `UseSqlite` to `UseSqlServer`/`UseNpgsql` in `Program.cs`
  and the connection string) — the unique-index approach to preventing
  clashes works identically on any of them.
- Switch `DbSeeder` from `EnsureCreatedAsync()` to real EF migrations
  (`dotnet ef migrations add InitialCreate`, then `Database.MigrateAsync()`)
  so future schema changes are versioned instead of a flat create.

## API reference

### Public

| Method | Path | Notes |
|---|---|---|
| GET | `/api/services` | Catalog of services |
| GET | `/api/barbers` | Catalog of barbers |
| GET | `/api/availability?date=&serviceId=&barberId=` | `barberId` optional. Returns 15-min-grid slots with `available: bool` |
| POST | `/api/bookings` | Create a booking. `409` on clash, `400` on validation failure |
| GET | `/api/bookings/{reference}` | Look up a booking by its `STOEP-...` reference (for the confirmation page) |

Example create request:

```json
POST /api/bookings
{
  "serviceId": "fade",
  "barberId": "neo",
  "date": "2026-10-02",
  "startMinutes": 570,
  "customerName": "Thabo Mokoena",
  "customerEmail": "thabo@example.com",
  "customerPhone": "0821234567",
  "notes": "First time, not too short on top"
}
```

### Barber dashboard (JWT-protected)

| Method | Path | Notes |
|---|---|---|
| POST | `/api/auth/login` | `{ username, password }` → JWT, valid 12h |
| GET | `/api/dashboard/bookings?from=&to=&barberId=` | A barber only ever sees their own; the owner account (`neo`) can pass `barberId` to filter or omit it to see everyone |
| PATCH | `/api/dashboard/bookings/{id}/status` | `{ "status": "Completed" \| "Cancelled" \| "NoShow" }` — cancelling/no-show frees the slot back up |

Send `Authorization: Bearer <token>` on both dashboard calls.

## Front-end changes

`stoep-frontend/assets/js/api.js` is a small fetch wrapper — point
`API_BASE` at wherever this API is hosted. `booking.js` was updated to call
`/api/availability` and `/api/bookings` instead of `localStorage`, and a new
`dashboard.html` + `dashboard.js` gives barbers a login screen and a table of
their upcoming bookings with "mark done / no-show / cancel" actions.
