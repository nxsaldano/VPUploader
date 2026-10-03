using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using VPUploader.Core.Models;

namespace VPUploader.Core;

public class PinterestClient
{
    private const string BaseUrl = "https://api.pinterest.com/v5";
    
    private readonly ITokenProvider _tokenProvider;
    private readonly HttpClient _http;
    
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
    
    public PinterestClient(ITokenProvider tokenProvider, HttpClient? httpClient = null)
    {
        _tokenProvider = tokenProvider;
        _http = httpClient ?? new HttpClient();
    }
    
    public async Task<List<Board>> GetBoardsAsync()
    {
        var boards = new List<Board>();
        string? bookmark = null;

        do
        {
            string url = $"{BaseUrl}/boards?page_size=100" +
                         (bookmark != null ? $"&bookmark={Uri.EscapeDataString(bookmark)}" : "");

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            await AttachAuthAsync(request);

            using var response = await _http.SendAsync(request);
            string body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"Failed to fetch boards ({(int)response.StatusCode}): {body}");

            var page = JsonSerializer.Deserialize<Board.BoardListResponse>(body, JsonOpts) ?? new Board.BoardListResponse();

            boards.AddRange(page.Items);
            bookmark = page.Bookmark;
        }
        while (!string.IsNullOrEmpty(bookmark));

        return boards;
    }

    private async Task AttachAuthAsync(HttpRequestMessage request)
    {
        string token = await _tokenProvider.GetValidAccessTokenAsync();
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
    
    public async Task<string> CreatePinAsync(PinRequest request)
    {
        string base64Image = Convert.ToBase64String(await File.ReadAllBytesAsync(request.ImagePath));
        string contentType = GuessContentType(request.ImagePath);

        var payload = new PinCreatePayload
        {
            BoardId = request.BoardId,
            Title = string.IsNullOrWhiteSpace(request.Title) ? null : request.Title,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description,
            Link = string.IsNullOrWhiteSpace(request.Link) ? null : request.Link,
            AltText = string.IsNullOrWhiteSpace(request.AltText) ? null : request.AltText,
            MediaSource = new MediaSource
            {
                SourceType = "image_base64",
                ContentType = contentType,
                Data = base64Image,
            },
        };

        string json = JsonSerializer.Serialize(payload, JsonOpts);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/pins")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
        await AttachAuthAsync(httpRequest);

        using var response = await _http.SendAsync(httpRequest);
        string responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Failed to create pin ({(int)response.StatusCode}): {responseBody}");

        using var doc = JsonDocument.Parse(responseBody);
        return doc.RootElement.GetProperty("id").GetString() ?? "(unknown id)";
    }
    
    private static string GuessContentType(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".png" => "image/png",
        ".gif" => "image/gif",
        ".bmp" => "image/bmp",
        _ => "image/jpeg",
    };
    
    private class PinCreatePayload
    {
        public required string BoardId { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Link { get; set; }
        public string? AltText { get; set; }
        public required MediaSource MediaSource { get; set; }
    }
    
    
    private class MediaSource
    {
        public required string SourceType { get; set; }
        public required string ContentType { get; set; }
        public required string Data { get; set; }
    }
    
}