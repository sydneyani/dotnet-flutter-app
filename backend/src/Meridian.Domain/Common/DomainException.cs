namespace Meridian.Domain.Common;

/// <summary>
/// A business rule was broken. Callers should not persist the aggregate after this is thrown.
/// </summary>
public sealed class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }
}
