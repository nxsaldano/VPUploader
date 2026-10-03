using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace VPUploader.Core;

public class AuthService : ITokenProvider
{
    private const string AuthorizeUrl = "https://www.pinterest.com/oauth/";
    private const string TokenUrl = "https://api.pinterest.com/v5/oauth/token";
    private const string RedirectUri = "http://127.0.0.1:8756/callback/";
    private const string Scopes = "boards:read,pins:read,pins:write";
    
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly ITokenStore _tokenStore;
    private readonly HttpClient _http;
    
    private string? _accessToken;
    private DateTimeOffset _accessTokenExpiresAt = DateTimeOffset.MinValue;
    
    public AuthService(string clientId, string clientSecret, ITokenStore? tokenStore = null, HttpClient? httpClient = null)
    {
        _clientId = clientId;
        _clientSecret = clientSecret;
        _tokenStore = tokenStore ?? new DpapiTokenStore();
        _http = httpClient ?? new HttpClient();
    }

    public async Task<string> GetValidAccessTokenAsync()
    {
        if (_accessToken != null && DateTimeOffset.UtcNow < _accessTokenExpiresAt)
            return _accessToken;

        var savedRefreshToken = _tokenStore.LoadRefreshToken();
        if (savedRefreshToken != null)
        {
            try
            {
                await RefreshAccessTokenAsync(savedRefreshToken);
                return _accessToken!;
            }
            catch
            {
                _tokenStore.Clear();
            }
        }
        await RunAuthorizationFlowAsync();
        return _accessToken!;
    }

    private async Task RunAuthorizationFlowAsync()
	{	
		// a random, unguessable string generated fresh every time
		// prevents a CRSF attack
    	string state = Guid.NewGuid().ToString("N");

		// builds the login page URL by hand
    	string authUrl = $"{AuthorizeUrl}?client_id={Uri.EscapeDataString(_clientId)}" +
                      	$"&redirect_uri={Uri.EscapeDataString(RedirectUri)}" +
                      	$"&response_type=code" +
                      	$"&scope={Uri.EscapeDataString(Scopes)}" +
                      	$"&state={state}";

		
    	using var listener = new HttpListener();
    	listener.Prefixes.Add(RedirectUri);
    	listener.Start();

		// opens the default browser into authUrl
    	Process.Start(new ProcessStartInfo(authUrl) { UseShellExecute = true });

		// this waits forever until the request comes
		// TODO  
		// fix this with CancellationToken
    	var context = await listener.GetContextAsync();
    	var query = context.Request.QueryString;

    	string? returnedState = query["state"];
    	string? code = query["code"];
    	string? error = query["error"];

    	string responseHtml = error == null
        	? "<html><body>Authorization complete - you can close this tab.</body></html>"
        	: $"<html><body>Authorization failed: {WebUtility.HtmlEncode(error)}</body></html>";

    	byte[] buffer = Encoding.UTF8.GetBytes(responseHtml);
    	context.Response.ContentLength64 = buffer.Length;
    	await context.Response.OutputStream.WriteAsync(buffer);
    	context.Response.Close();
    	listener.Stop();

    	if (error != null)
        	throw new InvalidOperationException($"Pinterest authorization failed: {error}");
    	if (returnedState != state)
        	throw new InvalidOperationException("OAuth state mismatch - possible CSRF, aborting.");
    	if (string.IsNullOrEmpty(code))
        	throw new InvalidOperationException("No authorization code returned by Pinterest.");

    	await ExchangeCodeForTokenAsync(code);
	}
    	
	// takes an already saved refresh token and trades it for a new access token
	public async Task RefreshAccessTokenAsync(string refreshToken)
	{
    	var form = new Dictionary<string, string>
    	{
        	["grant_type"] = "refresh_token",
        	["refresh_token"] = refreshToken,
    	};

    	var tokenResponse = await PostTokenRequestAsync(form);

    	_accessToken = tokenResponse.AccessToken;
    	_accessTokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresIn - 60);

    	if (!string.IsNullOrEmpty(tokenResponse.RefreshToken))
        	_tokenStore.SaveRefreshToken(tokenResponse.RefreshToken);
	}

	// takes the one-time authorization code and trades it for the first access token
	// and a refresh token
	private async Task ExchangeCodeForTokenAsync(string code)
	{
    	var form = new Dictionary<string, string>
    	{
    	    ["grant_type"] = "authorization_code",
    	    ["code"] = code,
    	    ["redirect_uri"] = RedirectUri,
    	    ["continuous_refresh"] = "true",
    	};

    	var tokenResponse = await PostTokenRequestAsync(form);

    	_accessToken = tokenResponse.AccessToken;
    	_accessTokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresIn - 60);
    	_tokenStore.SaveRefreshToken(tokenResponse.RefreshToken!);
	}

	// this method is the one that actually does the request depending on the exchange that called it
	private async Task<TokenResponse> PostTokenRequestAsync(Dictionary<string, string> form)
	{
    	using var request = new HttpRequestMessage(HttpMethod.Post, TokenUrl)
    	{
        	Content = new FormUrlEncodedContent(form),
    	};

    	string basicAuth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_clientId}:{_clientSecret}"));
    	request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicAuth);

    	using var response = await _http.SendAsync(request);
    	string body = await response.Content.ReadAsStringAsync();

    	if (!response.IsSuccessStatusCode)
        	throw new InvalidOperationException($"Token request failed ({(int)response.StatusCode}): {body}");

    	var result = JsonSerializer.Deserialize<TokenResponse>(body, JsonOpts);
    	if (result?.AccessToken == null)
        	throw new InvalidOperationException($"Token response missing access_token: {body}");

    	return result;
	}

	private static readonly JsonSerializerOptions JsonOpts = new()
	{
    	PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
	};

	// this is a nested DTO for the token response
	private class TokenResponse
	{
    	public string? AccessToken { get; set; }
    	public string? RefreshToken { get; set; }
    	public int ExpiresIn { get; set; }
	}

}