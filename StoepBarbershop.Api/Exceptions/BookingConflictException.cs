namespace StoepBarbershop.Api.Exceptions;

// Thrown when the requested slot(s) got taken — by another customer racing
// the same request, or because the underlying data changed — between the
// client fetching availability and the booking actually being submitted.
public class BookingConflictException : Exception
{
    public BookingConflictException(string message) : base(message) { }
}

public class ValidationFailedException : Exception
{
    public ValidationFailedException(string message) : base(message) { }
}
