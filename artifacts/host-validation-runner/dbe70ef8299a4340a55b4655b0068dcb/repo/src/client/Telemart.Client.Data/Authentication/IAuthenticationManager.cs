using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Telemart.Client.Data.Authentication
{
    public interface IAuthenticationManager
    {
        string AccessToken { get; }

        string RefreshToken { get; set; }

        ClaimsPrincipal ClaimsPrincipal { get; }

        DateTime LastTimeTokenRefreshed { get; }

        void ThrowIfNotAuthenticated();

        Task<AuthResponse> AuthenticateAsync(AuthRequest request);

        Task RefreshTokensAsync(bool forceRefresh = false);

        void ClearTokens();
    }
}