using MediatR;
using Meridian.Application.Abstractions;
using Meridian.Application.Common;
using Meridian.Application.Contracts;

namespace Meridian.Application.Users;

public sealed record ListUsersQuery : IRequest<IReadOnlyList<UserSummaryDto>>;

public sealed class ListUsersHandler(ICurrentUser current, IUserRepository users) : IRequestHandler<ListUsersQuery, IReadOnlyList<UserSummaryDto>>
{
    public Task<IReadOnlyList<UserSummaryDto>> Handle(ListUsersQuery request, CancellationToken cancellationToken)
    {
        current.Require();
        return users.ListActiveAsync(cancellationToken);
    }
}
