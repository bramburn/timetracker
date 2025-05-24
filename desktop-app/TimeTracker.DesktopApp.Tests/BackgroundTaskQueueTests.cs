using NUnit.Framework;
using TimeTracker.DesktopApp;

namespace TimeTracker.DesktopApp.Tests;

[TestFixture]
public class BackgroundTaskQueueTests
{
    private BackgroundTaskQueue? _queue;

    [TearDown]
    public void TearDown()
    {
        _queue?.Dispose();
    }

    [Test]
    public void Constructor_InitializesCorrectly()
    {
        // Act
        _queue = new BackgroundTaskQueue();

        // Assert
        Assert.That(_queue, Is.Not.Null);
        Assert.That(_queue.Count, Is.EqualTo(0));
        Assert.That(_queue.IsCompleted, Is.False);
    }

    [Test]
    public void QueueBackgroundWorkItem_AddsWorkItem()
    {
        // Arrange
        _queue = new BackgroundTaskQueue();
        var workItem = new Func<CancellationToken, Task>(_ => Task.CompletedTask);

        // Act
        _queue.QueueBackgroundWorkItem(workItem);

        // Assert
        Assert.That(_queue.Count, Is.EqualTo(1));
    }

    [Test]
    public void QueueBackgroundWorkItem_NullWorkItem_ThrowsArgumentNullException()
    {
        // Arrange
        _queue = new BackgroundTaskQueue();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _queue.QueueBackgroundWorkItem(null!));
    }

    [Test]
    public void QueueBackgroundWorkItem_DisposedQueue_ThrowsObjectDisposedException()
    {
        // Arrange
        _queue = new BackgroundTaskQueue();
        _queue.Dispose();
        var workItem = new Func<CancellationToken, Task>(_ => Task.CompletedTask);

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => _queue.QueueBackgroundWorkItem(workItem));
    }

    [Test]
    public async Task DequeueAsync_ReturnsQueuedWorkItem()
    {
        // Arrange
        _queue = new BackgroundTaskQueue();
        var workItem = new Func<CancellationToken, Task>(_ => Task.CompletedTask);
        _queue.QueueBackgroundWorkItem(workItem);

        // Act
        var dequeuedItem = await _queue.DequeueAsync(CancellationToken.None);

        // Assert
        Assert.That(dequeuedItem, Is.EqualTo(workItem));
        Assert.That(_queue.Count, Is.EqualTo(0));
    }

    [Test]
    public async Task DequeueAsync_EmptyQueue_WaitsForItem()
    {
        // Arrange
        _queue = new BackgroundTaskQueue();
        var workItem = new Func<CancellationToken, Task>(_ => Task.CompletedTask);

        // Act - Start dequeue operation
        var dequeueTask = _queue.DequeueAsync(CancellationToken.None);

        // Add item after a short delay
        _ = Task.Run(async () =>
        {
            await Task.Delay(50);
            _queue.QueueBackgroundWorkItem(workItem);
        });

        var dequeuedItem = await dequeueTask;

        // Assert
        Assert.That(dequeuedItem, Is.EqualTo(workItem));
    }

    [Test]
    public async Task DequeueAsync_CancelledToken_ReturnsNull()
    {
        // Arrange
        _queue = new BackgroundTaskQueue();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var dequeuedItem = await _queue.DequeueAsync(cts.Token);

        // Assert
        Assert.That(dequeuedItem, Is.Null);
    }

    [Test]
    public async Task DequeueAsync_CompletedQueue_ReturnsNull()
    {
        // Arrange
        _queue = new BackgroundTaskQueue();
        _queue.CompleteAdding();

        // Act
        var dequeuedItem = await _queue.DequeueAsync(CancellationToken.None);

        // Assert
        Assert.That(dequeuedItem, Is.Null);
    }

    [Test]
    public void CompleteAdding_MarksQueueAsCompleted()
    {
        // Arrange
        _queue = new BackgroundTaskQueue();

        // Act
        _queue.CompleteAdding();

        // Assert
        Assert.That(_queue.IsCompleted, Is.True);
    }

    [Test]
    public async Task WorkItemExecution_ExecutesCorrectly()
    {
        // Arrange
        _queue = new BackgroundTaskQueue();
        var executed = false;
        var workItem = new Func<CancellationToken, Task>(_ =>
        {
            executed = true;
            return Task.CompletedTask;
        });

        _queue.QueueBackgroundWorkItem(workItem);

        // Act
        var dequeuedItem = await _queue.DequeueAsync(CancellationToken.None);
        await dequeuedItem!(CancellationToken.None);

        // Assert
        Assert.That(executed, Is.True);
    }

    [Test]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        _queue = new BackgroundTaskQueue();

        // Act & Assert - Should not throw
        _queue.Dispose();
        _queue.Dispose();
    }

    [Test]
    public async Task MultipleWorkItems_ProcessedInOrder()
    {
        // Arrange
        _queue = new BackgroundTaskQueue();
        var executionOrder = new List<int>();
        
        var workItem1 = new Func<CancellationToken, Task>(_ =>
        {
            executionOrder.Add(1);
            return Task.CompletedTask;
        });
        
        var workItem2 = new Func<CancellationToken, Task>(_ =>
        {
            executionOrder.Add(2);
            return Task.CompletedTask;
        });

        // Act
        _queue.QueueBackgroundWorkItem(workItem1);
        _queue.QueueBackgroundWorkItem(workItem2);

        var item1 = await _queue.DequeueAsync(CancellationToken.None);
        var item2 = await _queue.DequeueAsync(CancellationToken.None);

        await item1!(CancellationToken.None);
        await item2!(CancellationToken.None);

        // Assert
        Assert.That(executionOrder, Is.EqualTo(new[] { 1, 2 }));
    }
}
