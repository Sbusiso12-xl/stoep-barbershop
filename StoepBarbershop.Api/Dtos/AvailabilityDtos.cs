namespace StoepBarbershop.Api.Dtos;

public record TimeSlotDto(int Start, int End, bool Available);

public record AvailabilityResponse(string Date, string? BarberId, List<TimeSlotDto> Slots);
