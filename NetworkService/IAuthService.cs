using System;

public interface IAuthService
{
    event Action<string, string> OnAuthenticated; // steamId, authTicketHex.
    void Authenticate();
    void CancelAuthentication();
}