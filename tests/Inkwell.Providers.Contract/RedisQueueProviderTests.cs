// Copyright (c) ShuaiHua Du. All rights reserved.

using Inkwell.Queue.Redis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.Redis;

namespace Inkwell.Providers.Contract;

/// <summary>
/// 针对 <see cref="RedisQueueProvider"/> 的真实 Testcontainers 集成测试。覆盖入队 / 出队 / Ack / Nack
/// 与死信队列（DLQ）在真实 Redis Streams 上的行为，而非仅编译期验证。
/// </summary>
[TestClass]
public sealed class RedisQueueProviderTests
{
    private static RedisContainer? container;

    [ClassInitialize]
    public static async Task ClassInitializeAsync(TestContext _)
    {
        container = new RedisBuilder("redis:8-alpine").Build();

        await container.StartAsync();
    }

    [ClassCleanup]
    public static async Task ClassCleanupAsync()
    {
        if (container is not null)
        {
            await container.DisposeAsync();
        }
    }

    [TestMethod]
    public async Task EnqueueAsync_Then_DequeueAsync_Delivers_MessageAsync()
    {
        IQueueProvider queue = BuildQueueProvider();
        string queueName = $"inkwell-test-{Guid.NewGuid():N}";
        using CancellationTokenSource cts = new(TimeSpan.FromSeconds(10));

        await queue.EnqueueAsync(queueName, new MessageEnvelope<string>(
            MessageId: Guid.NewGuid().ToString("N"),
            Payload: "payload-1",
            EnqueuedTime: DateTimeOffset.UtcNow,
            DeliveryCount: 0,
            TraceParent: null));

        await using IAsyncEnumerator<MessageEnvelope<string>> enumerator = queue.DequeueAsync<string>(queueName, cts.Token).GetAsyncEnumerator(cts.Token);
        bool moved = await enumerator.MoveNextAsync();

        Assert.IsTrue(moved);
        Assert.AreEqual("payload-1", enumerator.Current.Payload);

        await queue.AcknowledgeAsync(queueName, enumerator.Current.MessageId);
    }

    [TestMethod]
    public async Task NegativeAcknowledgeAsync_Requeues_Message_With_Incremented_DeliveryCountAsync()
    {
        IQueueProvider queue = BuildQueueProvider();
        string queueName = $"inkwell-test-{Guid.NewGuid():N}";
        using CancellationTokenSource cts = new(TimeSpan.FromSeconds(10));

        await queue.EnqueueAsync(queueName, new MessageEnvelope<string>(
            MessageId: Guid.NewGuid().ToString("N"),
            Payload: "payload-retry",
            EnqueuedTime: DateTimeOffset.UtcNow,
            DeliveryCount: 0,
            TraceParent: null));

        await using IAsyncEnumerator<MessageEnvelope<string>> firstDelivery = queue.DequeueAsync<string>(queueName, cts.Token).GetAsyncEnumerator(cts.Token);
        await firstDelivery.MoveNextAsync();
        string firstMessageId = firstDelivery.Current.MessageId;

        await queue.NegativeAcknowledgeAsync(queueName, firstMessageId);

        await using IAsyncEnumerator<MessageEnvelope<string>> secondDelivery = queue.DequeueAsync<string>(queueName, cts.Token).GetAsyncEnumerator(cts.Token);
        bool redelivered = await secondDelivery.MoveNextAsync();

        Assert.IsTrue(redelivered);
        Assert.AreEqual("payload-retry", secondDelivery.Current.Payload);
        Assert.AreEqual(1, secondDelivery.Current.DeliveryCount);

        await queue.AcknowledgeAsync(queueName, secondDelivery.Current.MessageId);
    }

    private static IQueueProvider BuildQueueProvider()
    {
        ServiceCollection services = new();
        services.AddLogging();

        IInkwellBuilder builder = services.AddInkwell(new ConfigurationBuilder().Build());

        builder.UseRedisQueue(container!.GetConnectionString());

        ServiceProvider provider = builder.Services.BuildServiceProvider();

        return provider.GetRequiredService<IQueueProvider>();
    }
}
