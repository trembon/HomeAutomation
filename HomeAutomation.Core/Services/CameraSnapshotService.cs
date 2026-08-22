namespace HomeAutomation.Core.Services;

public enum CameraSnapshotSize
{
    Full,
    Thumbnail
}

public sealed record CameraSnapshot(byte[] Content, string ContentType);

public interface ICameraSnapshotService
{
    Task<CameraSnapshot> Capture(string? url, CameraSnapshotSize size, CancellationToken cancellationToken);
}

public sealed class CameraSnapshotService(IHttpClientFactory httpClientFactory, IFFmpegService ffmpegService) : ICameraSnapshotService
{
    private static readonly TimeSpan FullSnapshotTimeout = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan ThumbnailSnapshotTimeout = TimeSpan.FromSeconds(10);

    public async Task<CameraSnapshot> Capture(string? url, CameraSnapshotSize size, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new InvalidOperationException("The camera does not have a snapshot URL configured.");

        if (url.StartsWith("rtsp://", StringComparison.OrdinalIgnoreCase))
            return await CaptureRtsp(url, size, cancellationToken);

        if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return await CaptureHttp(url, cancellationToken);
        }

        throw new InvalidOperationException("Camera snapshot URLs must use HTTP, HTTPS, or RTSP.");
    }

    private async Task<CameraSnapshot> CaptureHttp(string url, CancellationToken cancellationToken)
    {
        HttpClient client = httpClientFactory.CreateClient("cameras");
        using HttpResponseMessage response = await client.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        byte[] content = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        string contentType = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg";
        return new CameraSnapshot(content, contentType);
    }

    private async Task<CameraSnapshot> CaptureRtsp(string url, CameraSnapshotSize size, CancellationToken cancellationToken)
    {
        string[] arguments =
        [
            "-hide_banner",
            "-loglevel", "error",
            "-rtsp_transport", "tcp",
            "-i", url,
            "-frames:v", "1",
            "-an",
            "-f", "image2pipe",
            "-vcodec", "mjpeg",
            "pipe:1"
        ];

        TimeSpan timeout = size == CameraSnapshotSize.Thumbnail
            ? ThumbnailSnapshotTimeout
            : FullSnapshotTimeout;

        byte[] content = await ffmpegService.Execute(arguments, timeout, cancellationToken);
        return new CameraSnapshot(content, "image/jpeg");
    }
}
