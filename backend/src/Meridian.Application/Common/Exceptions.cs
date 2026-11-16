namespace Meridian.Application.Common;

public sealed class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }
}

public sealed class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message)
    {
    }
}

public sealed class ConflictException : Exception
{
    public ConflictException(string message) : base(message)
    {
    }
}

public sealed class UnauthorizedAppException : Exception
{
    public UnauthorizedAppException(string message) : base(message)
    {
    }
}
