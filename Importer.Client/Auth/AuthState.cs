using Importer.Core.Auth;

namespace Importer.Client.Auth;

public class AuthState
{
    public UserProfile? CurrentUser { get; private set; }

    public bool IsAuthenticated => CurrentUser != null;

    public void SetUser(UserProfile user)
        => CurrentUser = user;

    public void Clear()
        => CurrentUser = null;
}
