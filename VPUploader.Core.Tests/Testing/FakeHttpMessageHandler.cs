namespace VPUploader.Core.Tests;

using System.Net;
using System.Text;

public class FakeHttpMessageHandler : HttpMessageHandler
{
    
    private readonly Queue<HttpResponseMessage> _responses = new();

    public List<HttpRequestMessage> Requests { get; } = new();
    public List<string> RequestBodies { get; } = new();

    public void Enqueue(HttpStatusCode statusCode, string jsonBody)
    {
        _responses.Enqueue(new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(jsonBody, Encoding.UTF8, "application/json"),
        });
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        RequestBodies.Add(request.Content == null
            ? string.Empty
            : await request.Content.ReadAsStringAsync(cancellationToken));

        if (_responses.Count == 0)
            throw new InvalidOperationException(
                $"FakeHttpMessageHandler got a request to {request.RequestUri} but has no queued response left.");

        return _responses.Dequeue();
    }

    public HttpClient ToHttpClient() => new(this);
    
}