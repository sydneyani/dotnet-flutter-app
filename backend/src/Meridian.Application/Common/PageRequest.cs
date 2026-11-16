namespace Meridian.Application.Common;

public static class PageRequest
{
    public const int DefaultSize = 20;
    public const int MaxSize = 100;

    public static (int Page, int PageSize) Normalize(int page, int pageSize)
    {
        var normalizedPage = page < 1 ? 1 : page;
        var normalizedSize = pageSize < 1 ? DefaultSize : Math.Min(pageSize, MaxSize);
        return (normalizedPage, normalizedSize);
    }
}
