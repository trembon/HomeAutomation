namespace HomeAutomation.Core.Tests.TestSupport;

internal sealed class RecordingHttpMessageHandler(
    Func<RecordedHttpRequest, CancellationToken, Task<HttpResponseMessage>> responder) : HttpMessageHandler
{
    public List<RecordedHttpRequest> Requests { get; } = [];

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string? body = request.Content is null
            ? null
            : await request.Content.ReadAsStringAsync(cancellationToken);
        Dictionary<string, string> headers = request.Headers.ToDictionary(
            item => item.Key,
            item => string.Join(",", item.Value),
            StringComparer.OrdinalIgnoreCase);
        if (request.Content is not null)
        {
            foreach (KeyValuePair<string, IEnumerable<string>> header in request.Content.Headers)
                headers[header.Key] = string.Join(",", header.Value);
        }

        RecordedHttpRequest recordedRequest = new(
            request.Method,
            request.RequestUri!,
            headers,
            body);
        Requests.Add(recordedRequest);
        return await responder(recordedRequest, cancellationToken);
    }
}

internal sealed record RecordedHttpRequest(
    HttpMethod Method,
    Uri Uri,
    IReadOnlyDictionary<string, string> Headers,
    string? Body);
