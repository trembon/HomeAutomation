using HomeAutomation.API;
using HomeAutomation.Core.Tests.TestSupport;
using HomeAutomation.Database.Entities;
using HomeAutomation.Database.Enums;
using HomeAutomation.Database.Repositories;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using System.Net;

namespace HomeAutomation.Core.Tests.Webhooks;

public class CameraControllerTests
{
    [Fact]
    public async Task Capture_WhenDeviceDoesNotExist_ReturnsNotFound()
    {
        IDeviceRepository repository = Substitute.For<IDeviceRepository>();
        repository.GetById(123, Arg.Any<CancellationToken>()).Returns((DeviceEntity?)null);
        IHttpClientFactory httpClientFactory = Substitute.For<IHttpClientFactory>();
        CameraController controller = new(repository, httpClientFactory);

        IActionResult result = await controller.Index(123, "full", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
        _ = httpClientFactory.DidNotReceiveWithAnyArgs().CreateClient(default!);
    }

    [Fact]
    public async Task Capture_Thumbnail_UsesThumbnailUrlAndReturnsImage()
    {
        IDeviceRepository repository = Repository(Camera());
        StubHttpMessageHandler handler = new((_, _) =>
        {
            HttpResponseMessage response = new(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent([1, 2, 3])
            };
            return Task.FromResult(response);
        });
        CameraController controller = new(repository, HttpClientFactory(handler));

        IActionResult result = await controller.Index(1, "thumbnail", CancellationToken.None);

        FileStreamResult file = Assert.IsType<FileStreamResult>(result);
        Assert.Equal("image/png", file.ContentType);
        Assert.Equal("https://camera.test/thumbnail.png", handler.LastRequestUri?.AbsoluteUri);
        await file.FileStream.DisposeAsync();
    }

    [Fact]
    public async Task Capture_WhenCameraRequestFails_ReturnsNotFound()
    {
        CameraController controller = new(
            Repository(Camera()),
            HttpClientFactory(StubHttpMessageHandler.Respond(HttpStatusCode.BadGateway)));

        IActionResult result = await controller.Index(1, "full", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    private static IDeviceRepository Repository(DeviceEntity? device)
    {
        IDeviceRepository repository = Substitute.For<IDeviceRepository>();
        repository.GetById(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(device);
        return repository;
    }

    private static IHttpClientFactory HttpClientFactory(HttpMessageHandler handler)
    {
        IHttpClientFactory factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(handler));
        return factory;
    }

    private static DeviceEntity Camera()
    {
        return new DeviceEntity
        {
            Id = 1,
            Name = "Camera",
            Source = DeviceSource.ONVIF,
            SourceId = "camera",
            Kind = DeviceKind.Camera,
            Url = "https://camera.test/full.png",
            ThumbnailUrl = "https://camera.test/thumbnail.png"
        };
    }
}
