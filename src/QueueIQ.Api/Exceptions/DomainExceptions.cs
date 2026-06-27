namespace QueueIQ.Api.Exceptions;

/// <summary>
/// Base exception for domain-level errors in QueueIQ.
/// Caught by ApiExceptionMiddleware and mapped to appropriate HTTP status codes.
/// </summary>
public class DomainException : Exception
{
    public int StatusCode { get; }

    public DomainException(string message, int statusCode = 400) : base(message)
    {
        StatusCode = statusCode;
    }
}

/// <summary>Thrown when a requested entity is not found.</summary>
public class NotFoundException : DomainException
{
    public NotFoundException(string entity, object id)
        : base($"{entity} with identifier '{id}' was not found.", 404) { }
}

/// <summary>Thrown when an invalid ticket status transition is attempted.</summary>
public class InvalidStatusTransitionException : DomainException
{
    public InvalidStatusTransitionException(string from, string to)
        : base($"Cannot transition ticket from '{from}' to '{to}'.", 400) { }
}

/// <summary>Thrown when a concurrency conflict occurs (e.g., double-calling next).</summary>
public class ConcurrencyConflictException : DomainException
{
    public ConcurrencyConflictException()
        : base("The record was modified by another request. Please retry.", 409) { }

    public ConcurrencyConflictException(string message)
        : base(message, 409) { }
}

/// <summary>Thrown when a slug is already taken.</summary>
public class SlugConflictException : DomainException
{
    public SlugConflictException(string slug)
        : base($"The slug '{slug}' is already in use.", 409) { }
}
