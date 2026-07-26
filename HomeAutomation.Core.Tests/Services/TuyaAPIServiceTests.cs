using HomeAutomation.Core.Services;
using HomeAutomation.Core.Tests.TestSupport;
using HomeAutomation.Database.Enums;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using System.Net;

namespace HomeAutomation.Core.Tests.Services;

public class TuyaAPIServiceTests
{
    [Fact]
    public void ConvertStateToDps_ForWhiteLight_UsesSeparateBrightnessAndTemperatureFields()
    {
        TuyaAPIService service = CreateService(StubHttpMessageHandler.Respond(HttpStatusCode.OK));

        Dictionary<int, object> result = service.ConvertStateToDPS(
            DeviceEvent.On,
            DeviceKind.Lightbulb,
            new Dictionary<string, string>
            {
                ["brightness"] = "25",
                ["temperature"] = "75"
            });

        Assert.Equal(true, result[20]);
        Assert.Equal("white", result[21]);
        Assert.Equal(250, result[22]);
        Assert.Equal(750, result[23]);
    }

    [Theory]
    [InlineData(DeviceKind.Lightbulb, 20, true, DeviceEvent.On)]
    [InlineData(DeviceKind.Lightbulb, 20, false, DeviceEvent.Off)]
    [InlineData(DeviceKind.PowerSwitch, 1, true, DeviceEvent.On)]
    [InlineData(DeviceKind.PowerSwitch, 1, false, DeviceEvent.Off)]
    public void ConvertPropertyToEvent_MapsSupportedDeviceStates(
        DeviceKind kind,
        int property,
        bool value,
        DeviceEvent expected)
    {
        TuyaAPIService service = CreateService(StubHttpMessageHandler.Respond(HttpStatusCode.OK));

        DeviceEvent result = service.ConvertPropertyToEvent(
            kind,
            new Dictionary<int, object> { [property] = value });

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task SendCommand_SendsExpectedJsonAndEscapedDeviceId()
    {
        StubHttpMessageHandler handler = StubHttpMessageHandler.Respond(HttpStatusCode.NoContent);
        TuyaAPIService service = CreateService(handler);

        bool result = await service.SendCommand(
            "device/one",
            new Dictionary<int, object> { [1] = true },
            CancellationToken.None);

        Assert.True(result);
        Assert.Equal(HttpMethod.Post, handler.LastMethod);
        Assert.Equal("https://tuya.test/devices/device%2Fone/send", handler.LastRequestUri?.AbsoluteUri);
        Assert.Contains("\"1\":true", handler.LastContent, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetDevices_WithEmptyResponse_ReturnsEmptyCollection()
    {
        StubHttpMessageHandler handler = StubHttpMessageHandler.Respond(HttpStatusCode.OK);
        TuyaAPIService service = CreateService(handler);

        IEnumerable<HomeAutomation.Core.Models.TuyaDeviceModel> result =
            await service.GetDevices(CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetDevices_WithFailureStatus_Throws()
    {
        TuyaAPIService service = CreateService(
            StubHttpMessageHandler.Respond(HttpStatusCode.BadGateway));

        await Assert.ThrowsAsync<HttpRequestException>(
            () => service.GetDevices(CancellationToken.None));
    }

    private static TuyaAPIService CreateService(HttpMessageHandler handler)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Tuya:APIUrl"] = "https://tuya.test/"
            })
            .Build();

        IHttpClientFactory httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(handler));
        return new TuyaAPIService(configuration, httpClientFactory);
    }
}
