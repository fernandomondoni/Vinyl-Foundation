namespace Vinyl.Identity.Application.Abstractions;

public interface IAuthenticatedUserContextAccessor
{
    AuthenticatedUserContext? Current { get; }
}
