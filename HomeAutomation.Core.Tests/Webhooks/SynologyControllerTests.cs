using HomeAutomation.Core.Services;
using HomeAutomation.Webhooks;
using HomeAutomation.Webhooks.Models.Synology;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using System.Net;

namespace HomeAutomation.Core.Tests.Webhooks;

public class SynologyControllerTests
{
    [Fact]
    public async Task Notification_WhenSourceIsNotAllowed_ReturnsForbidden()
    {
        var notificationService = Substitute.For<ISynologyNotificationService>();
        var controller = CreateController(notificationService);

        var result = await controller.Notification(new NotificationModel { Text = "Message" }, CancellationToken.None);

        Assert.Equal(StatusCodes.Status403Forbidden, Assert.IsType<StatusCodeResult>(result).StatusCode);
        await notificationService.DidNotReceiveWithAnyArgs().ForwardToSlack(default!, default);
    }

    [Fact]
    public async Task Notification_WhenMessageIsEmpty_ReturnsBadRequest()
    {
        var notificationService = Substitute.For<ISynologyNotificationService>();
        notificationService.IsSourceAllowed(Arg.Any<IPAddress>()).Returns(true);
        var controller = CreateController(notificationService);

        var result = await controller.Notification(new NotificationModel { Text = " " }, CancellationToken.None);

        Assert.IsType<BadRequestResult>(result);
        await notificationService.DidNotReceiveWithAnyArgs().ForwardToSlack(default!, default);
    }

    [Fact]
    public async Task Notification_WhenSlackAcceptsMessage_ReturnsOk()
    {
        var notificationService = Substitute.For<ISynologyNotificationService>();
        notificationService.IsSourceAllowed(Arg.Any<IPAddress>()).Returns(true);
        notificationService.ForwardToSlack("Message", Arg.Any<CancellationToken>()).Returns(true);
        var controller = CreateController(notificationService);

        var result = await controller.Notification(new NotificationModel { Text = "Message" }, CancellationToken.None);

        Assert.IsType<OkResult>(result);
        await notificationService.Received(1).ForwardToSlack("Message", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Notification_WhenSlackRejectsMessage_ReturnsBadGateway()
    {
        var notificationService = Substitute.For<ISynologyNotificationService>();
        notificationService.IsSourceAllowed(Arg.Any<IPAddress>()).Returns(true);
        notificationService.ForwardToSlack("Message", Arg.Any<CancellationToken>()).Returns(false);
        var controller = CreateController(notificationService);

        var result = await controller.Notification(new NotificationModel { Text = "Message" }, CancellationToken.None);

        Assert.Equal(StatusCodes.Status502BadGateway, Assert.IsType<StatusCodeResult>(result).StatusCode);
    }

    private static SynologyController CreateController(ISynologyNotificationService notificationService)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse("192.168.1.20");

        return new SynologyController(
            notificationService,
            NullLogger<SynologyController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            }
        };
    }
}
