namespace CarvedRockFitness.API.DTOs;

public record ErrorResponse(
    string Type,
    string Title,
    int Status,
    string? Detail,
    string TraceId,
    IDictionary<string, string[]>? Errors = null);

public record ValidationErrorResponse(
    string Type,
    string Title,
    int Status,
    string TraceId,
    IDictionary<string, string[]> Errors) : ErrorResponse(Type, Title, Status, null, TraceId, Errors);

