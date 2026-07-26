using HomeAutomation.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using System.Net;

namespace HomeAutomation.Core.Tests.Services;

public class SynologyNotificationServiceTests
{
    [Theory]
    [InlineData("192.168.1.20")]
    [InlineData("2001:db8::20")]
    public void IsSourceAllowed_WhenAddressIsConfigured_ReturnsTrue(string address)
    {
        var sut = CreateService(
            Substitute.For<INotificationService>(),
            new Dictionary<string, string?>
            {
                ["Synology:AllowedIpAddresses:0"] = address
            });

        Assert.True(sut.IsSourceAllowed(IPAddress.Parse(address)));
    }

    [Fact]
    public void IsSourceAllowed_WhenIpv4AddressIsMappedToIpv6_ReturnsTrue()
    {
        var sut = CreateService(
            Substitute.For<INotificationService>(),
            new Dictionary<string, string?>
            {
                ["Synology:AllowedIpAddresses:0"] = "192.168.1.20"
            });

        Assert.True(sut.IsSourceAllowed(IPAddress.Parse("::ffff:192.168.1.20")));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("192.168.1.21")]
    public void IsSourceAllowed_WhenAddressIsNotConfigured_ReturnsFalse(string? address)
    {
        var sut = CreateService(
            Substitute.For<INotificationService>(),
            new Dictionary<string, string?>
            {
                ["Synology:AllowedIpAddresses:0"] = "192.168.1.20"
            });

        Assert.False(sut.IsSourceAllowed(address is null ? null : IPAddress.Parse(address)));
    }

    [Fact]
    public void IsSourceAllowed_WhenConfiguredAddressIsMalformed_ReturnsFalse()
    {
        var sut = CreateService(
            Substitute.For<INotificationService>(),
            new Dictionary<string, string?>
            {
                ["Synology:AllowedIpAddresses:0"] = "not-an-ip-address"
            });

        Assert.False(sut.IsSourceAllowed(IPAddress.Parse("192.168.1.20")));
    }

    [Fact]
    public void IsSourceAllowed_WhenNoAddressesAreConfigured_ReturnsFalse()
    {
        var sut = CreateService(
            Substitute.For<INotificationService>(),
            new Dictionary<string, string?>());

        Assert.False(sut.IsSourceAllowed(IPAddress.Parse("192.168.1.20")));
    }

    [Fact]
    public async Task ForwardToSlack_FormatsMessageAndUsesConfiguredChannel()
    {
        var notificationService = Substitute.For<INotificationService>();
        notificationService
            .SendToSlack("nas-errors", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);
        var sut = CreateService(
            notificationService,
            new Dictionary<string, string?>
            {
                ["Synology:SlackChannel"] = "nas-errors"
            });

        bool result = await sut.ForwardToSlack("  Storage pool degraded.  ", CancellationToken.None);

        Assert.True(result);
        await notificationService.Received(1).SendToSlack(
            "nas-errors",
            ":warning: *Synology NAS*\nStorage pool degraded.",
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ForwardToSlack_WhenChannelIsMissing_UsesErrors(string? configuredChannel)
    {
        var notificationService = Substitute.For<INotificationService>();
        notificationService
            .SendToSlack("errors", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);
        var values = new Dictionary<string, string?>();
        if (configuredChannel is not null)
            values["Synology:SlackChannel"] = configuredChannel;
        var sut = CreateService(notificationService, values);

        bool result = await sut.ForwardToSlack("Message", CancellationToken.None);

        Assert.True(result);
        await notificationService.Received(1).SendToSlack(
            "errors",
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ForwardToSlack_WhenSlackFails_ReturnsFalse()
    {
        var notificationService = Substitute.For<INotificationService>();
        notificationService
            .SendToSlack("errors", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);
        var sut = CreateService(notificationService, new Dictionary<string, string?>());

        bool result = await sut.ForwardToSlack("Message", CancellationToken.None);

        Assert.False(result);
    }

    private static SynologyNotificationService CreateService(
        INotificationService notificationService,
        IReadOnlyDictionary<string, string?> values)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

        return new SynologyNotificationService(
            configuration,
            notificationService,
            NullLogger<SynologyNotificationService>.Instance);
    }
}
