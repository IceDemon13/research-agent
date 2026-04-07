using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using IdentityModel.Client;
using Microsoft.Extensions.Logging;
using Telemart.Client.Core.Exceptions;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.Data.Authentication
{
    public sealed class AuthenticationManager : IAuthenticationManager
    {
        private const string ClientId = "telemart.client";
        private const string ClientSecret = "fN8BuO8TDGOGjrjzobvwd3Yn9JR0ZPEZ";
        private const string Scope = "openid offline_access telemart_service_api telemart_report_api telemart_prices_api telemart_jobs_api telemart_catalog_api telemart_fiscal_registrar_api telemart_telegram_api telemart_call_api";

        private static readonly HashSet<string> IdClaimTypes = new HashSet<string>(StringComparer.Ordinal)
        {
            "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name",
            "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier",
            "sub"
        };

        private readonly TimeSpan _refreshInterval = TimeSpan.FromMinutes(30);
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<AuthenticationManager> _logger;

        public AuthenticationManager(IHttpClientFactory httpClientFactory, ILogger<AuthenticationManager> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            LastTimeTokenRefreshed = DateTime.MinValue;
        }

        public string AccessToken { get; private set; }

        public string RefreshToken { get; set; }

        public ClaimsPrincipal ClaimsPrincipal { get; private set; }

        public DateTime LastTimeTokenRefreshed { get; set; }

        public void ThrowIfNotAuthenticated()
        {
            if (string.IsNullOrWhiteSpace(AccessToken) || string.IsNullOrWhiteSpace(RefreshToken))
            {
                throw new InvalidOperationException();
            }
        }

        public async Task<AuthResponse> AuthenticateAsync(AuthRequest request)
        {
            ClearTokens();

            HttpClient client = _httpClientFactory.CreateClient(Services.Identity.ToString());

            PasswordTokenRequest tokenRequest = new PasswordTokenRequest
            {
                Address = "connect/token",
                ClientId = ClientId,
                ClientSecret = ClientSecret,
                Scope = Scope,

                UserName = request.Login,
                Password = request.Password
            };

            TokenResponse tokenResponse = await client.RequestPasswordTokenAsync(tokenRequest);

            if (!tokenResponse.IsError)
            {
                JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();

                JwtSecurityToken jwtToken = handler.ReadJwtToken(tokenResponse.AccessToken);

                // ReSharper disable once PossibleUnintendedLinearSearchInSet
                int userId = int.Parse(jwtToken.Claims
                    .First(x => IdClaimTypes
                                    .Contains(x.Type, StringComparer.OrdinalIgnoreCase)
                                && int.TryParse(x.Value, out int _)).Value);

                SetTokens(tokenResponse);
                return new AuthResponse(userId);
            }

            AuthResponse authResponse;

            switch (tokenResponse.HttpStatusCode)
            {
                case HttpStatusCode.InternalServerError:
                    _logger.LogError(tokenResponse.Exception, "Failed to authenticate user");
                    authResponse = new AuthResponse("Ошибка аутентификации");
                    break;

                case HttpStatusCode.Unauthorized:
                    _logger.LogWarning("Failed to authenticate user. Error: {Error}", tokenResponse.Error);
                    authResponse = new AuthResponse("Неверный логин/пароль или учетная запись заблокирована");
                    break;

                default:
                    _logger.LogError(
                        "Failed to authenticate user. HttpErrorReason: {ErrorReason}. HttpErrorStatusCode: {StatusCode}",
                        tokenResponse.HttpErrorReason,
                        tokenResponse.HttpStatusCode);
                    authResponse = new AuthResponse("Ошибка связи с сервером");
                    break;
            }

            return authResponse;
        }

        public async Task RefreshTokensAsync(bool forceRefresh = false)
        {
            await _semaphore.WaitAsync();

            try
            {
                if (DateTime.Now - LastTimeTokenRefreshed <= _refreshInterval && !forceRefresh)
                {
                    return;
                }

                _logger.LogInformation("Start Refresh token");

                HttpClient client = _httpClientFactory.CreateClient(Services.Identity.ToString());

                RefreshTokenRequest tokenRequest = new RefreshTokenRequest
                {
                    Address = "connect/token",
                    ClientId = ClientId,
                    ClientSecret = ClientSecret,

                    RefreshToken = RefreshToken
                };

                TokenResponse tokenResponse = await client.RequestRefreshTokenAsync(tokenRequest);

                if (tokenResponse.IsError)
                {
                    _logger.LogWarning("Failed to refresh token. Error: {Error}", tokenResponse.Error);

                    Error error = new Error() { ErrorCode = ErrorCode.None, ErrorMessage = "Authentication failed", Details = new List<Error>() };

                    throw new UnexpectedSatusException(HttpStatusCode.Unauthorized, error);
                }

                SetTokens(tokenResponse);

                _logger.LogInformation("End Refresh token");
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public void ClearTokens()
        {
            AccessToken = null;
            RefreshToken = null;
            ClaimsPrincipal = null;

            LastTimeTokenRefreshed = DateTime.MinValue;
        }

        private void SetTokens(TokenResponse tokenResponse)
        {
            AccessToken = tokenResponse.AccessToken;
            RefreshToken = tokenResponse.RefreshToken;

            JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
            JwtSecurityToken jwtSecurityToken = tokenHandler.ReadJwtToken(AccessToken);

            ClaimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(jwtSecurityToken.Claims, "jwt"));

            LastTimeTokenRefreshed = DateTime.Now;
        }
    }
}