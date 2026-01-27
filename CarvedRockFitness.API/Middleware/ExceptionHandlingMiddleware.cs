using System.Diagnostics;
using System.Text.Json;
using CarvedRockFitness.API.DTOs;
using CarvedRockFitness.API.Exceptions;

namespace CarvedRockFitness.API.Middleware;

public class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger,
    IHostEnvironment environment)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

        var (statusCode, title, type) = exception switch
        {
            NotFoundException => (404, "Not Found", "https://tools.ietf.org/html/rfc7231#section-6.5.4"),
            BadRequestException => (400, "Bad Request", "https://tools.ietf.org/html/rfc7231#section-6.5.1"),
            ConflictException => (409, "Conflict", "https://tools.ietf.org/html/rfc7231#section-6.5.8"),
            UnauthorizedException => (401, "Unauthorized", "https://tools.ietf.org/html/rfc7235#section-3.1"),
            ForbiddenException => (403, "Forbidden", "https://tools.ietf.org/html/rfc7231#section-6.5.3"),
            _ => (500, "Internal Server Error", "https://tools.ietf.org/html/rfc7231#section-6.6.1")
        };

        logger.LogError(exception, "An exception occurred. TraceId: {TraceId}, StatusCode: {StatusCode}", traceId, statusCode);

        var detail = environment.IsDevelopment() ? exception.Message : null;
        if (statusCode == 500 && !environment.IsDevelopment())
            detail = "An unexpected error occurred. Please try again later.";
        else if (exception is ApiException)
            detail = exception.Message;

        var errorResponse = new ErrorResponse(type, title, statusCode, detail, traceId);

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse, JsonOptions));
    }
}

public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
        => app.UseMiddleware<ExceptionHandlingMiddleware>();
}

