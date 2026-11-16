using FluentValidation;
using MediatR;
using Meridian.Application.Abstractions;
using Meridian.Application.Common;
using Meridian.Application.Contracts;

namespace Meridian.Application.Auth;

public sealed record LoginCommand(string Email, string Password) : IRequest<AuthTokensDto>;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(200);
    }
}

public sealed class LoginHandler(
    IUserRepository users,
    IPasswordHasher passwords,
    ITokenService tokens,
    IRefreshTokenFactory refreshTokens,
    IRefreshTokenRepository refreshTokenRepository,
    IClock clock,
    IUnitOfWork unitOfWork) : IRequestHandler<LoginCommand, AuthTokensDto>
{
    public async Task<AuthTokensDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        UserAccount? user;
        try
        {
            user = await users.GetByEmailAsync(UserAccount.NormalizeEmail(request.Email), cancellationToken);
        }
        catch (DomainException)
        {
            user = null;
        }

        if (user is null || !user.IsActive || !passwords.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAppException("Invalid email or password.");
        }

        return await IssueAsync(user, tokens, refreshTokens, refreshTokenRepository, clock, unitOfWork, cancellationToken);
    }

    internal static async Task<AuthTokensDto> IssueAsync(
        UserAccount user,
        ITokenService tokens,
        IRefreshTokenFactory refreshTokens,
        IRefreshTokenRepository refreshTokenRepository,
        IClock clock,
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var refreshToken = refreshTokens.GenerateToken();
        var expiresAt = clock.UtcNow.AddDays(tokens.RefreshTokenDays);
        await refreshTokenRepository.AddAsync(
            RefreshToken.Issue(user.Id, refreshTokens.Hash(refreshToken), clock.UtcNow, expiresAt),
            cancellationToken);

        var (accessToken, accessExpiresAt) = tokens.CreateAccessToken(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new AuthTokensDto(accessToken, accessExpiresAt, refreshToken, expiresAt, ToSummary(user));
    }

    internal static UserSummaryDto ToSummary(UserAccount user) =>
        new(user.Id, user.Email, user.DisplayName, user.Role);
}

public sealed record RefreshSessionCommand(string RefreshToken) : IRequest<AuthTokensDto>;

public sealed class RefreshSessionCommandValidator : AbstractValidator<RefreshSessionCommand>
{
    public RefreshSessionCommandValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty().MaximumLength(512);
    }
}

public sealed class RefreshSessionHandler(
    IUserRepository users,
    ITokenService tokens,
    IRefreshTokenFactory refreshTokens,
    IRefreshTokenRepository refreshTokenRepository,
    IClock clock,
    IUnitOfWork unitOfWork) : IRequestHandler<RefreshSessionCommand, AuthTokensDto>
{
    public async Task<AuthTokensDto> Handle(RefreshSessionCommand request, CancellationToken cancellationToken)
    {
        var existing = await refreshTokenRepository.GetByHashAsync(refreshTokens.Hash(request.RefreshToken.Trim()), cancellationToken);
        if (existing is null)
        {
            throw new UnauthorizedAppException("Refresh token is invalid.");
        }

        if (existing.RevokedAt is not null)
        {
            await refreshTokenRepository.RevokeAllForUserAsync(existing.UserId, clock.UtcNow, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            throw new UnauthorizedAppException("Refresh token was reused. Sign in again.");
        }

        if (!existing.IsActive(clock.UtcNow))
        {
            throw new UnauthorizedAppException("Refresh token has expired.");
        }

        var user = await users.GetByIdAsync(existing.UserId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            throw new UnauthorizedAppException("Refresh token is invalid.");
        }

        var replacement = refreshTokens.GenerateToken();
        existing.Revoke(clock.UtcNow, refreshTokens.Hash(replacement));
        var expiresAt = clock.UtcNow.AddDays(tokens.RefreshTokenDays);
        await refreshTokenRepository.AddAsync(
            RefreshToken.Issue(user.Id, refreshTokens.Hash(replacement), clock.UtcNow, expiresAt),
            cancellationToken);

        var (accessToken, accessExpiresAt) = tokens.CreateAccessToken(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new AuthTokensDto(accessToken, accessExpiresAt, replacement, expiresAt, LoginHandler.ToSummary(user));
    }
}

public sealed record LogoutCommand(string RefreshToken) : IRequest;

public sealed class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
    public LogoutCommandValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty().MaximumLength(512);
    }
}

public sealed class LogoutHandler(
    IRefreshTokenFactory refreshTokens,
    IRefreshTokenRepository refreshTokenRepository,
    IClock clock,
    IUnitOfWork unitOfWork) : IRequestHandler<LogoutCommand>
{
    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var existing = await refreshTokenRepository.GetByHashAsync(refreshTokens.Hash(request.RefreshToken.Trim()), cancellationToken);
        if (existing is null || existing.RevokedAt is not null)
        {
            return;
        }

        existing.Revoke(clock.UtcNow, null);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

public sealed record GetMeQuery : IRequest<UserSummaryDto>;

public sealed class GetMeHandler(ICurrentUser current, IUserRepository users) : IRequestHandler<GetMeQuery, UserSummaryDto>
{
    public async Task<UserSummaryDto> Handle(GetMeQuery request, CancellationToken cancellationToken)
    {
        var (userId, _) = current.Require();
        var user = await users.GetByIdAsync(userId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            throw new UnauthorizedAppException("Sign in is required.");
        }

        return LoginHandler.ToSummary(user);
    }
}
