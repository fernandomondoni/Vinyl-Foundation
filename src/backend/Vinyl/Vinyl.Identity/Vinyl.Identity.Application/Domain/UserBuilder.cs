using Vinyl.Identity.Application.Abstractions;

namespace Vinyl.Identity.Application.Domain;

public sealed class UserBuilder
{
    private Guid? _id;
    private ExternalIdentity? _externalIdentity;
    private string? _email;
    private string? _displayName;
    private DateTimeOffset _now;

    public UserBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public UserBuilder FromAuthenticatedContext(AuthenticatedUserContext context, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(context);

        _externalIdentity = new ExternalIdentity(context.Issuer, context.Subject, context.Provider);
        _email = context.Email;
        _displayName = context.DisplayName;
        _now = now;
        return this;
    }

    public User Build()
    {
        if (_externalIdentity is null)
        {
            throw new InvalidOperationException("An external identity is required to build a user.");
        }

        if (_now == default)
        {
            throw new InvalidOperationException("A creation timestamp is required to build a user.");
        }

        return User.Register(
            _id ?? Guid.NewGuid(),
            _externalIdentity,
            new UserProfile(_email, _displayName),
            _now);
    }
}
