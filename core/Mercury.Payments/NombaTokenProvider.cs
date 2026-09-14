using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Mercury.Payments;

public interface INombaTokenProvider
{
    public Task<string> GetAccessTokenAsync(CancellationToken ct);
}

public class NombaTokenProvider(IHttpClientFactory httpClientFactory, IOptions<NombaOptions> options): INombaTokenProvider
{
    private readonly NombaOptions _options = options.Value;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private string? _cachedToken;
    private DateTime _expiresAtUtc = DateTime.MinValue; 
    
    public async Task<string> GetAccessTokenAsync(CancellationToken ct)
    {
        if (_cachedToken is not null && _expiresAtUtc > DateTime.UtcNow) return _cachedToken;
        
        // Create a semaphore lock for the async section below
        await _lock.WaitAsync(ct);
        try
        {
            if (_cachedToken is not null && _expiresAtUtc > DateTime.UtcNow) return _cachedToken;
            var client =  httpClientFactory.CreateClient("Nomba");
            var request = new HttpRequestMessage(HttpMethod.Post, "v1/auth/token/issue");
            request.Headers.Add("accountId", _options.AccountId);
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = _options.ClientId,
                ["client_secret"] = _options.PrivateKey
            });
            var response = await client.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();
            
            var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
            // var code = body.GetProperty("code").GetString();
            
            var data = body.GetProperty("data");
            
            _cachedToken = data.GetProperty("access_token").GetString()!;
            var expiresAt = data.GetProperty("expiresAt").GetInt32();
            _expiresAtUtc = DateTime.UtcNow.AddSeconds(expiresAt - 60);

            return _cachedToken;
        }
        finally
        {
            _lock.Release();
        }
        
    }
}