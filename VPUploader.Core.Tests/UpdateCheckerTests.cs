using System.Net;
using Xunit;

namespace VPUploader.Core.Tests;

public class UpdateCheckerTests
{
    private const string ReleaseJson = """
                                       { "tag_name": "v1.2.0",
                                         "html_url": "https://github.com/nxsaldano/VPUploader/releases/tag/v1.2.0" }
                                       """;

    [Fact]
    public async Task CheckAsync_ReturnsUpdate_WhenLatestIsNewer()
    {
        var handler = new FakeHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, ReleaseJson);
        var checker = new UpdateChecker(handler.ToHttpClient());

        var update = await checker.CheckAsync(new Version(1, 0, 0, 0));

        Assert.NotNull(update);
        Assert.Equal(new Version(1, 2, 0), update.Version);
        Assert.EndsWith("v1.2.0", update.Url);
    }

    [Fact]
    public async Task CheckAsync_ReturnsNull_WhenAlreadyUpToDate()
    {
        var handler = new FakeHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, ReleaseJson);
        var checker = new UpdateChecker(handler.ToHttpClient());

        var update = await checker.CheckAsync(new Version(1, 2, 0, 0));

        Assert.Null(update);
    }

    [Fact]
    public async Task CheckAsync_ReturnsNull_WhenGitHubReturnsError()
    {
        var handler = new FakeHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.NotFound, "{}");
        var checker = new UpdateChecker(handler.ToHttpClient());

        var update = await checker.CheckAsync(new Version(1, 0, 0, 0));

        Assert.Null(update);
    }
}