namespace Inkwell.Tests.Queue;

[TestClass]
public sealed class InMemoryPubSubProviderTests
{
    [TestMethod]
    public async Task PublishAndSubscribe_DeliversMessage()
    {
        // Arrange
        InMemoryPubSubProvider<string> provider = new();
        string? received = null;
        TaskCompletionSource tcs = new();

        await using IAsyncDisposable sub = await provider.SubscribeAsync("ch1", (msg, ct) =>
        {
            received = msg;
            tcs.SetResult();
            return Task.CompletedTask;
        });

        // Act
        await provider.PublishAsync("hello", "ch1");
        await Task.WhenAny(tcs.Task, Task.Delay(2000));

        // Assert
        Assert.AreEqual("hello", received);
    }

    [TestMethod]
    public async Task Subscribe_DifferentChannels_AreIsolated()
    {
        // Arrange
        InMemoryPubSubProvider<string> provider = new();
        string? ch1Msg = null;
        string? ch2Msg = null;

        await using IAsyncDisposable sub1 = await provider.SubscribeAsync("ch1", (msg, ct) =>
        {
            ch1Msg = msg;
            return Task.CompletedTask;
        });

        await using IAsyncDisposable sub2 = await provider.SubscribeAsync("ch2", (msg, ct) =>
        {
            ch2Msg = msg;
            return Task.CompletedTask;
        });

        // Act
        await provider.PublishAsync("only-ch1", "ch1");
        await Task.Delay(100);

        // Assert
        Assert.AreEqual("only-ch1", ch1Msg);
        Assert.IsNull(ch2Msg);
    }

    [TestMethod]
    public async Task Unsubscribe_StopsDelivery()
    {
        // Arrange
        InMemoryPubSubProvider<string> provider = new();
        int callCount = 0;

        IAsyncDisposable sub = await provider.SubscribeAsync("ch1", (msg, ct) =>
        {
            callCount++;
            return Task.CompletedTask;
        });

        // Act
        await provider.PublishAsync("msg1", "ch1");
        await Task.Delay(100);
        await sub.DisposeAsync();
        await provider.PublishAsync("msg2", "ch1");
        await Task.Delay(100);

        // Assert
        Assert.AreEqual(1, callCount);
    }
}
