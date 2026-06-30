using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Gateway.Blazor.Services;

namespace Gateway.Blazor.Security;

/// <summary>
/// Handler HTTP personalizado que intercepta requisições de saída e anexa o token JWT nos cabeçalhos.
/// </summary>
public class AuthHeaderHandler : DelegatingHandler
{
    private readonly LocalStorageService _localStorage;
    private const string AuthTokenKey = "authToken";

    public AuthHeaderHandler(LocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _localStorage.GetItemAsync<string>(AuthTokenKey);

        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
