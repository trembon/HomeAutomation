using HomeAutomation.API;
using HomeAutomation.Core.Services;
using HomeAutomation.Database.Entities;
using HomeAutomation.Database.Enums;
using HomeAutomation.Database.Repositories;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace HomeAutomation.Core.Tests.Webhooks;

public class CameraControllerTests
{
    [Fact]
    public async Task Capture_WhenDeviceDoesNotExist_ReturnsNotFound()
    {
        IDeviceRepository repository = Substitute.For<IDeviceRepository>();
        repository.GetById(123, Arg.Any<CancellationToken>()).Returns((DeviceEntity?)null);
        ICameraSnapshotService snapshotService = Substitute.For<ICameraSnapshotService>();
        CameraController controller = new(repository, snapshotService);

        IActionResult result = await controller.Index(123, "full", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
        _ = snapshotService.DidNotReceiveWithAnyArgs().Capture(default, default, default);
    }

    [Fact]
    public async Task Capture_Thumbnail_UsesThumbnailUrlAndReturnsImage()
    {
        IDeviceRepository repository = Repository(Camera());
        ICameraSnapshotService snapshotService = Substitute.For<ICameraSnapshotService>();
        snapshotService
            .Capture("https://camera.test/thumbnail.png", CameraSnapshotSize.Thumbnail, Arg.Any<CancellationToken>())
            .Returns(new CameraSnapshot([1, 2, 3], "image/png"));
        CameraController controller = new(repository, snapshotService);

        IActionResult result = await controller.Index(1, "thumbnail", CancellationToken.None);

        FileContentResult file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("image/png", file.ContentType);
        Assert.Equal([1, 2, 3], file.FileContents);
        _ = snapshotService.Received(1).Capture(
            "https://camera.test/thumbnail.png",
            CameraSnapshotSize.Thumbnail,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Capture_WhenCameraRequestFails_ReturnsNotFound()
    {
        CameraController controller = new(
            Repository(Camera()),
            FailingSnapshotService());

        IActionResult result = await controller.Index(1, "full", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    private static IDeviceRepository Repository(DeviceEntity? device)
    {
        IDeviceRepository repository = Substitute.For<IDeviceRepository>();
        repository.GetById(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(device);
        return repository;
    }

    private static ICameraSnapshotService FailingSnapshotService()
    {
        ICameraSnapshotService service = Substitute.For<ICameraSnapshotService>();
        service
            .Capture(Arg.Any<string?>(), Arg.Any<CameraSnapshotSize>(), Arg.Any<CancellationToken>())
            .Returns<Task<CameraSnapshot>>(_ => throw new HttpRequestException());
        return service;
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
