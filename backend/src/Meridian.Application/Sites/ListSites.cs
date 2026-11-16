using MediatR;
using Meridian.Application.Abstractions;
using Meridian.Application.Common;
using Meridian.Application.Contracts;

namespace Meridian.Application.Sites;

public sealed record ListSitesQuery : IRequest<IReadOnlyList<SiteDto>>;

public sealed class ListSitesHandler(ICurrentUser current, ISiteRepository sites) : IRequestHandler<ListSitesQuery, IReadOnlyList<SiteDto>>
{
    public Task<IReadOnlyList<SiteDto>> Handle(ListSitesQuery request, CancellationToken cancellationToken)
    {
        current.Require();
        return sites.ListAsync(cancellationToken);
    }
}
