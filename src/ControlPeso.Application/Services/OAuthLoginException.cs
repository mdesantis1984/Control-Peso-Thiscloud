namespace ControlPeso.Application.Services;

/// <summary>
/// The OAuth sign-in cannot be completed safely (for example the email already belongs to
/// another account and the provider did not verify it). The login ticket must be rejected.
/// </summary>
public sealed class OAuthLoginException : InvalidOperationException
{
    public OAuthLoginException(string message)
        : base(message)
    {
    }
}
