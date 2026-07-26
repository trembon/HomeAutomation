using System.Net;

namespace HomeAutomation.Core.Tests.TestSupport;

internal sealed class StubHttpMessageHandler(
    Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder) : HttpMessageHandler
{
    public int RequestCount { get; private set; }

    public HttpMethod? LastMethod { get; private set; }

    public Uri? LastRequestUri { get; private set; }

    public string? LastContent { get; private set; }

    public static StubHttpMessageHandler Respond(
        HttpStatusCode statusCode,
        string? content = null,
        string mediaType = "application/json")
    {
        return new StubHttpMessageHandler((_, _) =>
        {
            HttpResponseMessage response = new(statusCode);
            if (content is not null)
                response.Content = new StringContent(content, System.Text.Encoding.UTF8, mediaType);

            return Task.FromResult(response);
        });
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        RequestCount++;
        LastMethod = request.Method;
        LastRequestUri = request.RequestUri;
        LastContent = request.Content is null
            ? null
            : await request.Content.ReadAsStringAsync(cancellationToken);

        return await responder(request, cancellationToken);
    }
}
