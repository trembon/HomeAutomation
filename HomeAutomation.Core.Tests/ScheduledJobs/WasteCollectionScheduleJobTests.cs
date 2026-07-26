using HomeAutomation.Core.ScheduledJobs;
using HomeAutomation.Core.Services;
using HomeAutomation.Database;
using HomeAutomation.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using System.Net;
using System.Text;

namespace HomeAutomation.Core.Tests.ScheduledJobs;

public class WasteCollectionScheduleJobTests
{
    private const string ApiUrl = "https://example.test/waste/addresses/test-address-id";
    private const string AddressId = "test-address-id";
    private const string UnitId = "configured-unit-id";

    [Fact]
    public async Task Fetch_WithEmptyDatabase_StoresCurrentServices()
    {
        using var context = CreateContext();
        var handler = new StubHttpMessageHandler(_ => JsonResponse(ApiResponse));
        var sut = CreateFetchJob(context, handler);

        await sut.Execute(new DateTime(2026, 7, 24, 0, 30, 0), null, CancellationToken.None);

        Assert.Equal(1, handler.RequestCount);
        Assert.Equal(2, await context.WasteCollections.CountAsync());
    }

    [Fact]
    public async Task Fetch_StoresCurrentServicesAndDeactivatesStaleRows()
    {
        using var context = CreateContext();
        var notifiedAt = new DateTime(2026, 7, 23, 10, 0, 0);
        var existingCurrent = Collection("Kärl 1", new DateOnly(2026, 8, 4), new DateTime(2026, 7, 23));
        existingCurrent.NotificationSentAt = notifiedAt;
        context.WasteCollections.AddRange(
            existingCurrent,
            new WasteCollectionEntity
            {
                ExternalAddressId = AddressId,
                Address = "Testvägen 6",
                City = "Teststad",
                PropertyId = "123456",
                Type = "Stale",
                Description = "No longer returned",
                CollectionDate = new DateOnly(2026, 8, 1),
                Precision = "day",
                FirstSeenAt = new DateTime(2026, 7, 22),
                LastSeenAt = new DateTime(2026, 7, 23),
                IsActive = true
            });
        await context.SaveChangesAsync();

        string? moduleHeader = null;
        string? unitHeader = null;
        var handler = new StubHttpMessageHandler(request =>
        {
            moduleHeader = request.Headers.GetValues("module").Single();
            unitHeader = request.Headers.GetValues("unit").Single();
            return JsonResponse(ApiResponse);
        });
        var sut = CreateFetchJob(context, handler);

        await sut.Execute(new DateTime(2026, 7, 24, 2, 0, 0), null, CancellationToken.None);

        var rows = await context.WasteCollections.OrderBy(x => x.Type).ToListAsync();
        Assert.Equal(3, rows.Count);
        Assert.False(rows.Single(x => x.Type == "Stale").IsActive);
        Assert.Equal(
            ["Kärl 1", "Kärl 2"],
            [.. rows.Where(x => x.IsActive).Select(x => x.Type).OrderBy(x => x)]);
        Assert.Equal(new DateOnly(2026, 8, 4), rows.Single(x => x.Type == "Kärl 1").CollectionDate);
        Assert.Equal(new DateOnly(2026, 8, 11), rows.Single(x => x.Type == "Kärl 2").CollectionDate);
        Assert.Equal(notifiedAt, rows.Single(x => x.Type == "Kärl 1").NotificationSentAt);
        Assert.Equal("universal", moduleHeader);
        Assert.Equal(UnitId, unitHeader);
        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public async Task Fetch_WhenAlreadyFetchedToday_DoesNotCallApiAgain()
    {
        using var context = CreateContext();
        context.WasteCollections.Add(Collection("Kärl 1", new DateOnly(2026, 8, 4), new DateTime(2026, 7, 24, 2, 0, 0)));
        await context.SaveChangesAsync();

        var handler = new StubHttpMessageHandler(_ => JsonResponse(ApiResponse));
        var sut = CreateFetchJob(context, handler);

        await sut.Execute(new DateTime(2026, 7, 24, 12, 0, 0), null, CancellationToken.None);

        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public async Task Notify_AtConfiguredHour_SendsOneMessageAndMarksAllCollections()
    {
        using var context = CreateContext();
        context.WasteCollections.AddRange(
            Collection("Kärl 1", new DateOnly(2026, 8, 4), new DateTime(2026, 7, 24, 2, 0, 0)),
            Collection("Kärl 2", new DateOnly(2026, 8, 4), new DateTime(2026, 7, 24, 2, 0, 0)));
        await context.SaveChangesAsync();

        var notificationService = Substitute.For<INotificationService>();
        notificationService.SendToSlack("events", Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        var sut = CreateNotifyJob(context, notificationService);

        await sut.Execute(new DateTime(2026, 8, 3, 10, 0, 0), null, CancellationToken.None);
        await sut.Execute(new DateTime(2026, 8, 3, 10, 5, 0), null, CancellationToken.None);

        await notificationService.Received(1).SendToSlack(
            "events",
            Arg.Is<string>(message => message.Contains("Kärl 1") && message.Contains("Kärl 2")),
            Arg.Any<CancellationToken>());
        Assert.All(await context.WasteCollections.ToListAsync(), x => Assert.NotNull(x.NotificationSentAt));
    }

    [Fact]
    public async Task Notify_WhenSlackFails_LeavesNotificationPending()
    {
        using var context = CreateContext();
        context.WasteCollections.Add(Collection("Kärl 1", new DateOnly(2026, 8, 4), new DateTime(2026, 7, 24, 2, 0, 0)));
        await context.SaveChangesAsync();

        var notificationService = Substitute.For<INotificationService>();
        notificationService.SendToSlack("events", Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        var sut = CreateNotifyJob(context, notificationService);

        await sut.Execute(new DateTime(2026, 8, 3, 10, 0, 0), null, CancellationToken.None);

        Assert.Null((await context.WasteCollections.SingleAsync()).NotificationSentAt);
    }

    [Fact]
    public async Task Notify_BeforeConfiguredHour_DoesNotSend()
    {
        using var context = CreateContext();
        context.WasteCollections.Add(Collection("Kärl 1", new DateOnly(2026, 8, 4), new DateTime(2026, 7, 24, 2, 0, 0)));
        await context.SaveChangesAsync();

        var notificationService = Substitute.For<INotificationService>();
        var sut = CreateNotifyJob(context, notificationService);

        await sut.Execute(new DateTime(2026, 8, 3, 9, 59, 0), null, CancellationToken.None);

        _ = notificationService.DidNotReceiveWithAnyArgs().SendToSlack(default!, default!, default);
    }

    private static FetchWasteCollectionScheduleJob CreateFetchJob(DefaultContext context, HttpMessageHandler handler)
    {
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(handler));

        return new FetchWasteCollectionScheduleJob(
            context,
            httpClientFactory,
            CreateConfiguration(new Dictionary<string, string?>
            {
                ["WasteCollection:ApiUrl"] = ApiUrl,
                ["WasteCollection:UnitId"] = UnitId,
                ["WasteCollection:Module"] = "universal",
                ["WasteCollection:FetchHour"] = "2"
            }),
            NullLogger<FetchWasteCollectionScheduleJob>.Instance);
    }

    private static NotifyWasteCollectionScheduleJob CreateNotifyJob(DefaultContext context, INotificationService notificationService)
    {
        return new NotifyWasteCollectionScheduleJob(
            context,
            notificationService,
            CreateConfiguration(new Dictionary<string, string?>
            {
                ["WasteCollection:NotificationHour"] = "10",
                ["WasteCollection:SlackChannel"] = "events"
            }),
            NullLogger<NotifyWasteCollectionScheduleJob>.Instance);
    }

    private static IConfiguration CreateConfiguration(IReadOnlyDictionary<string, string?> values)
    {
        var configuration = Substitute.For<IConfiguration>();
        configuration[Arg.Any<string>()].Returns(call => values.GetValueOrDefault(call.Arg<string>()));

        foreach (var (key, value) in values)
        {
            var section = Substitute.For<IConfigurationSection>();
            section.Value.Returns(value);
            configuration.GetSection(key).Returns(section);
        }

        return configuration;
    }

    private static WasteCollectionEntity Collection(string type, DateOnly collectionDate, DateTime lastSeenAt)
    {
        return new WasteCollectionEntity
        {
            ExternalAddressId = AddressId,
            Address = "Testvägen 6",
            City = "Teststad",
            PropertyId = "123456",
            Type = type,
            Description = $"Description for {type}",
            CollectionDate = collectionDate,
            Precision = "day",
            FirstSeenAt = lastSeenAt,
            LastSeenAt = lastSeenAt,
            IsActive = true
        };
    }

    private static HttpResponseMessage JsonResponse(string json)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private static TestDefaultContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<DefaultContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TestDefaultContext(options);
    }

    private sealed class TestDefaultContext(DbContextOptions<DefaultContext> options) : DefaultContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<ActionEntity>().Ignore(x => x.DeviceEventProperties);
        }
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            return Task.FromResult(responder(request));
        }
    }

    private const string ApiResponse = """
        {
          "message": "Successfully fetched item",
          "status": 200,
          "data": {
            "uuid": "test-address-id",
            "alias": "testvaegen-6-teststad",
            "address": "Testvägen 6",
            "city": "Teststad",
            "property_id": "123456",
            "services": [
              {
                "description": "Fyrfack 370 liter, Kärl 2 var 4:e vecka",
                "type": "Kärl 2",
                "collection_at": "2026-08-11",
                "precision": "day"
              },
              {
                "description": "Fyrfack 370 liter, Kärl 1 varannan vecka",
                "type": "Kärl 1",
                "collection_at": "2026-08-04",
                "precision": "day"
              }
            ]
          }
        }
        """;
}
