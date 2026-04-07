using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Telemart.Client.Data.Authentication;

public class AuthenticationHttpClientMessageHandler : DelegatingHandler
{
    private readonly IAuthenticationManager _authenticationManager;

    public AuthenticationHttpClientMessageHandler(IAuthenticationManager authenticationManager)
    {
        _authenticationManager = authenticationManager;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _authenticationManager.ThrowIfNotAuthenticated();

        await _authenticationManager.RefreshTokensAsync().ConfigureAwait(false);

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _authenticationManager.AccessToken);

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}