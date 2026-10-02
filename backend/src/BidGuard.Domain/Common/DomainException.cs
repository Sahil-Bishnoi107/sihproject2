namespace BidGuard.Domain.Common;

/// <summary>Raised when an operation would violate a domain invariant (e.g. an invalid state transition).</summary>
public class DomainException(string message) : Exception(message);
