namespace CarvedRockFitness.API.Exceptions;

public abstract class ApiException(string message, int statusCode) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

public class NotFoundException(string message) : ApiException(message, 404);

public class BadRequestException(string message) : ApiException(message, 400);

public class ConflictException(string message) : ApiException(message, 409);

public class UnauthorizedException(string message) : ApiException(message, 401);

public class ForbiddenException(string message) : ApiException(message, 403);

