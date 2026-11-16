using MediatR;
using Meridian.Application.Abstractions;
using Meridian.Application.Common;
using Meridian.Application.Contracts;

namespace Meridian.Application.Dashboard;

public sealed record GetDashboardQuery : IRequest<DashboardSummaryDto>;

public sealed class GetDashboardHandler(ICurrentUser current, IDashboardReader dashboard)
    : IRequestHandler<GetDashboardQuery, DashboardSummaryDto>
{
    public Task<DashboardSummaryDto> Handle(GetDashboardQuery request, CancellationToken cancellationToken)
    {
        current.Require();
        return dashboard.GetSummaryAsync(cancellationToken);
    }
}
