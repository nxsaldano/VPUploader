using System.Net;
using System.Text.Json;
using VPUploader.Core.Models;
using VPUploader.Core.Tests.Testing;
using Xunit;

namespace VPUploader.Core.Tests;

public class PinterestClientTests
{
    
    // this is a helper method for the test
    private static string CreateTempImageFile()
    {
        string path = Path.Combine(Path.GetTempPath(), $"vpuploader-test-{Guid.NewGuid():N}.jpg");
        File.WriteAllBytes(path, new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }); // minimal JPEG-ish header, content doesn't matter
        return path;
    }

    // the [Fact] attribute tells xUnit that this method is a test that must be run once dotnet test is called
    [Fact]
    public async Task CreatePinAsync_SendsExpectedFields_AndReturnsPinId()
    {
        var handler = new FakeHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.Created, """{ "id": "pin_12345" }""");

        var client = new PinterestClient(new FakeTokenProvider("token-abc"), handler.ToHttpClient());
        string imagePath = CreateTempImageFile();

        try
        {
            string pinId = await client.CreatePinAsync(new PinRequest
            {
                BoardId = "board_1",
                ImagePath = imagePath,
                Title = "My title",
                Description = "My description",
                Link = "https://example.com",
                AltText = "My alt text",
            });

            Assert.Equal("pin_12345", pinId);
            Assert.Single(handler.Requests);

            var request = handler.Requests[0];
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("https://api.pinterest.com/v5/pins", request.RequestUri!.ToString());
            Assert.Equal("Bearer", request.Headers.Authorization!.Scheme);
            Assert.Equal("token-abc", request.Headers.Authorization!.Parameter);

            using var body = JsonDocument.Parse(handler.RequestBodies[0]);
            var root = body.RootElement;
            Assert.Equal("board_1", root.GetProperty("board_id").GetString());
            Assert.Equal("My title", root.GetProperty("title").GetString());
            Assert.Equal("My description", root.GetProperty("description").GetString());
            Assert.Equal("https://example.com", root.GetProperty("link").GetString());
            Assert.Equal("My alt text", root.GetProperty("alt_text").GetString());
            Assert.Equal("image_base64", root.GetProperty("media_source").GetProperty("source_type").GetString());
            Assert.Equal("image/jpeg", root.GetProperty("media_source").GetProperty("content_type").GetString());
        }
        finally
        {
            File.Delete(imagePath);
        }
    }
    
    [Fact]
    public async Task CreatePinAsync_OmitsOptionalFields_WhenNotProvided()
    {
        var handler = new FakeHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.Created, """{ "id": "pin_999" }""");

        var client = new PinterestClient(new FakeTokenProvider(), handler.ToHttpClient());
        string imagePath = CreateTempImageFile();

        try
        {
            // Only the required fields - title/description/link/alt text all left out.
            await client.CreatePinAsync(new PinRequest { BoardId = "board_1", ImagePath = imagePath });

            using var body = JsonDocument.Parse(handler.RequestBodies[0]);
            var root = body.RootElement;

            Assert.False(root.TryGetProperty("title", out _));
            Assert.False(root.TryGetProperty("description", out _));
            Assert.False(root.TryGetProperty("link", out _));
            Assert.False(root.TryGetProperty("alt_text", out _));
        }
        finally
        {
            File.Delete(imagePath);
        }
    }
    
    [Fact]
    public async Task CreatePinAsync_Throws_WhenPinterestReturnsError()
    {
        var handler = new FakeHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.BadRequest, """{ "message": "board_id is invalid" }""");

        var client = new PinterestClient(new FakeTokenProvider(), handler.ToHttpClient());
        string imagePath = CreateTempImageFile();

        try
        {
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                client.CreatePinAsync(new PinRequest { BoardId = "bad-board", ImagePath = imagePath }));

            Assert.Contains("board_id is invalid", ex.Message);
        }
        finally
        {
            File.Delete(imagePath);
        }
    }
    
    [Fact]
    public async Task GetBoardsAsync_FollowsBookmark_AcrossMultiplePages()
    {
        var handler = new FakeHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, """
                                           { "items": [ { "id": "b1", "name": "Board One" } ], "bookmark": "page2" }
                                           """);
        handler.Enqueue(HttpStatusCode.OK, """
                                           { "items": [ { "id": "b2", "name": "Board Two" } ], "bookmark": null }
                                           """);

        var client = new PinterestClient(new FakeTokenProvider(), handler.ToHttpClient());

        List<Board> boards = await client.GetBoardsAsync();

        Assert.Equal(2, boards.Count);
        Assert.Equal("b1", boards[0].Id);
        Assert.Equal("b2", boards[1].Id);

        Assert.Equal(2, handler.Requests.Count);
        Assert.Contains("bookmark=page2", handler.Requests[1].RequestUri!.ToString());
    }
    
}