using System.Text.Json;
using System.Text.Json.Serialization;

namespace VPUploader.Core;

public record UpdateInfo(Version Version, string Url);

public class UpdateChecker
{
    private const string LatestReleaseUrl =
        "https://api.github.com/repos/nxsaldano/VPUploader/releases/latest";

    private readonly HttpClient _http;

    public UpdateChecker(HttpClient? httpClient = null)
    {
        _http = httpClient ?? new HttpClient();
    }

    public async Task<UpdateInfo?> CheckAsync(Version currentVersion)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, LatestReleaseUrl);
        // GitHub rejects requests that don't identify themselves
        request.Headers.UserAgent.ParseAdd("VPUploader");

        using var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return null;

        string body = await response.Content.ReadAsStringAsync();
        var release = JsonSerializer.Deserialize<ReleaseResponse>(body);
        if (release?.TagName == null || release.HtmlUrl == null)
            return null;

        // tags look like "v1.0.1", and Version can't parse the leading "v"
        if (!Version.TryParse(release.TagName.TrimStart('v'), out var latest))
            return null;

        return Normalize(latest) > Normalize(currentVersion)
            ? new UpdateInfo(Normalize(latest), release.HtmlUrl)
            : null;
    }

    // .NET reports "1.0.0" as 1.0.0.0, and Version treats a missing part as
    // smaller than 0, so we keep exactly three parts before comparing
    private static Version Normalize(Version v) =>
        new(v.Major, v.Minor, Math.Max(v.Build, 0));

    // DTO for just the two fields we need from GitHub's JSON
    private class ReleaseResponse
    {
        [JsonPropertyName("tag_name")] public string? TagName { get; set; }
        [JsonPropertyName("html_url")] public string? HtmlUrl { get; set; }
    }
}