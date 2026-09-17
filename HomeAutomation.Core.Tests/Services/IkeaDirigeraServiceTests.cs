using HomeAutomation.Core.Services;
using HomeAutomation.Core.Tests.TestSupport;
using HomeAutomation.Database.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using System.Net;
using System.Text.Json;

namespace HomeAutomation.Core.Tests.Services;

public class IkeaDirigeraServiceTests
{
    [Theory]
    [InlineData(DeviceEvent.On, true)]
    [InlineData(DeviceEvent.Off, false)]
    public void ConvertStateToAction_MapsPowerState(DeviceEvent state, bool expectedIsOn)
    {
        IkeaDirigeraService service = CreateService();

        JsonElement attributes = GetAttributes(service.ConvertStateToActions(state, DeviceKind.PowerSwitch, []).Single());

        Assert.Equal(expectedIsOn, attributes.GetProperty("isOn").GetBoolean());
        Assert.Single(attributes.EnumerateObject());
    }

    [Fact]
    public void ConvertStateToAction_MapsHexColorToHueSaturationAndBrightness()
    {
        IkeaDirigeraService service = CreateService();

        IReadOnlyList<object> actions = service.ConvertStateToActions(
            DeviceEvent.On,
            DeviceKind.Lightbulb,
            new Dictionary<string, string> { ["color"] = "#FF0000" });

        Assert.Equal(3, actions.Count);
        JsonElement colorAttributes = GetAttributes(actions[1]);
        JsonElement brightnessAttributes = GetAttributes(actions[2]);
        Assert.Equal(0, colorAttributes.GetProperty("colorHue").GetDouble());
        Assert.Equal(1, colorAttributes.GetProperty("colorSaturation").GetDouble());
        Assert.Equal(100, brightnessAttributes.GetProperty("lightLevel").GetInt32());
    }

    [Fact]
    public void ConvertStateToAction_MapsBrightnessAndTemperature()
    {
        IkeaDirigeraService service = CreateService();

        IReadOnlyList<object> actions = service.ConvertStateToActions(
            DeviceEvent.On,
            DeviceKind.Lightbulb,
            new Dictionary<string, string>
            {
                ["brightness"] = "75",
                ["temperature"] = "50"
            });

        Assert.Equal(3, actions.Count);
        Assert.Equal(4369, GetAttributes(actions[1]).GetProperty("colorTemperature").GetInt32());
        Assert.Equal(75, GetAttributes(actions[2]).GetProperty("lightLevel").GetInt32());
    }

    [Theory]
    [InlineData("0", 2202)]
    [InlineData("100", 6535)]
    public void ConvertStateToAction_MapsTemperatureEndpoints(string temperature, int expectedTemperature)
    {
        IkeaDirigeraService service = CreateService();

        IReadOnlyList<object> actions = service.ConvertStateToActions(
            DeviceEvent.On,
            DeviceKind.Lightbulb,
            new Dictionary<string, string> { ["temperature"] = temperature });

        Assert.Equal(expectedTemperature, GetAttributes(actions[1]).GetProperty("colorTemperature").GetInt32());
    }

    [Theory]
    [InlineData("color", "red")]
    [InlineData("brightness", "101")]
    [InlineData("temperature", "-1")]
    [InlineData("temperature", "warm")]
    public void ConvertStateToAction_RejectsInvalidLightProperties(string property, string value)
    {
        IkeaDirigeraService service = CreateService();

        Assert.Throws<ArgumentException>(() => service.ConvertStateToActions(
            DeviceEvent.On,
            DeviceKind.Lightbulb,
            new Dictionary<string, string> { [property] = value }));
    }

    [Fact]
    public void ConvertStateToAction_ReturnsNullForUnsupportedState()
    {
        IkeaDirigeraService service = CreateService();

        Assert.Empty(service.ConvertStateToActions(DeviceEvent.Unknown, DeviceKind.Lightbulb, []));
    }

    private static IkeaDirigeraService CreateService()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["IkeaDirigera:IP"] = "dirigera.test",
                ["IkeaDirigera:Token"] = "token"
            })
            .Build();

        IHttpClientFactory httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(StubHttpMessageHandler.Respond(HttpStatusCode.OK)));
        return new IkeaDirigeraService(configuration, httpClientFactory, NullLogger<IkeaDirigeraService>.Instance);
    }

    private static JsonElement GetAttributes(object? payload)
    {
        Assert.NotNull(payload);
        using JsonDocument document = JsonDocument.Parse(JsonSerializer.Serialize(payload));
        return document.RootElement[0].GetProperty("attributes").Clone();
    }
}
