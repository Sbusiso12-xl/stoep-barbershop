using System.Net;
using System.Text.Json;

namespace StoepBarbershop.Api.Middleware;

// Catches anything that slips past the controllers' own try/catch blocks
// (a dropped DB connection, a bug, whatever) and guarantees the customer
// never sees a stack trace or a bare "Internal Server Error" — while the
// real detail still gets logged server-side so we can actually debug it.
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            // Same {error} shape every other error response uses, so the
            // front end never needs a special case for "something we didn't
            // anticipate" — it just shows the message like any other.
            var payload = JsonSerializer.Serialize(new
            {
                error = "Something went wrong on our end. Please try again in a moment, and give us a call if it keeps happening."
            });
            await context.Response.WriteAsync(payload);
        }
    }
}
