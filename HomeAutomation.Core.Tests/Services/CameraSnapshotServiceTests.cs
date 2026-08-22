using HomeAutomation.Core.Services;
using HomeAutomation.Core.Tests.TestSupport;
using NSubstitute;
using System.Net;
using System.Net.Http.Headers;

namespace HomeAutomation.Core.Tests.Services;

public class CameraSnapshotServiceTests
{
    [Fact]
    public async Task Capture_HttpUrl_ReturnsDownloadedImage()
    {
        StubHttpMessageHandler handler = new((_, _) =>
        {
            HttpResponseMessage response = new(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent([1, 2, 3])
            };
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            return Task.FromResult(response);
        });
        IHttpClientFactory httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory.CreateClient("cameras").Returns(new HttpClient(handler));
        IFFmpegService ffmpegService = Substitute.For<IFFmpegService>();
        CameraSnapshotService service = new(httpClientFactory, ffmpegService);

        CameraSnapshot result = await service.Capture(
            "https://camera.test/snapshot.png",
            CameraSnapshotSize.Full,
            CancellationToken.None);

        Assert.Equal([1, 2, 3], result.Content);
        Assert.Equal("image/png", result.ContentType);
        _ = ffmpegService.DidNotReceiveWithAnyArgs().Execute(default!, default, default);
    }

    [Fact]
    public async Task Capture_RtspUrl_UsesConfiguredArgumentsAndFullTimeout()
    {
        IHttpClientFactory httpClientFactory = Substitute.For<IHttpClientFactory>();
        IFFmpegService ffmpegService = Substitute.For<IFFmpegService>();
        ffmpegService
            .Execute(Arg.Any<IEnumerable<string>>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns([0xFF, 0xD8, 0xFF]);
        CameraSnapshotService service = new(httpClientFactory, ffmpegService);
        const string url = "rtsp://admin:secret@192.168.0.116:8554/Streaming/Channels/101";

        CameraSnapshot result = await service.Capture(url, CameraSnapshotSize.Full, CancellationToken.None);

        Assert.Equal("image/jpeg", result.ContentType);
        _ = ffmpegService.Received(1).Execute(
            Arg.Is<IEnumerable<string>>(arguments => arguments.SequenceEqual(
            new[]
            {
                "-hide_banner",
                "-loglevel", "error",
                "-rtsp_transport", "tcp",
                "-i", url,
                "-frames:v", "1",
                "-an",
                "-f", "image2pipe",
                "-vcodec", "mjpeg",
                "pipe:1"
            })),
            TimeSpan.FromSeconds(15),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Capture_RtspThumbnail_UsesShorterTimeout()
    {
        IHttpClientFactory httpClientFactory = Substitute.For<IHttpClientFactory>();
        IFFmpegService ffmpegService = Substitute.For<IFFmpegService>();
        ffmpegService
            .Execute(Arg.Any<IEnumerable<string>>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns([0xFF, 0xD8, 0xFF]);
        CameraSnapshotService service = new(httpClientFactory, ffmpegService);

        await service.Capture(
            "rtsp://camera.test/Streaming/Channels/102",
            CameraSnapshotSize.Thumbnail,
            CancellationToken.None);

        _ = ffmpegService.Received(1).Execute(
            Arg.Any<IEnumerable<string>>(),
            TimeSpan.FromSeconds(10),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Capture_UnsupportedUrl_Throws()
    {
        CameraSnapshotService service = new(
            Substitute.For<IHttpClientFactory>(),
            Substitute.For<IFFmpegService>());

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.Capture(
            "ftp://camera.test/snapshot.jpg",
            CameraSnapshotSize.Full,
            CancellationToken.None));
    }
}
