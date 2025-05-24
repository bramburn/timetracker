using System.Collections.Concurrent;

namespace TimeTracker.DesktopApp;

/// <summary>
/// Generic background task queue for offloading work items to background processing.
/// Uses BlockingCollection to manage producer-consumer pattern for asynchronous work execution.
/// </summary>
public class BackgroundTaskQueue : IDisposable
{
    private readonly BlockingCollection<Func<CancellationToken, Task>> _workItems = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private bool _disposed = false;

    /// <summary>
    /// Queues a background work item for execution
    /// </summary>
    /// <param name="workItem">The work item to execute</param>
    public void QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem)
    {
        if (workItem == null)
            throw new ArgumentNullException(nameof(workItem));

        if (_disposed)
            throw new ObjectDisposedException(nameof(BackgroundTaskQueue));

        _workItems.Add(workItem);
    }

    /// <summary>
    /// Dequeues and returns the next work item to execute
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The next work item to execute, or null if the queue is completed</returns>
    public async Task<Func<CancellationToken, Task>?> DequeueAsync(CancellationToken cancellationToken)
    {
        try
        {
            var combinedToken = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, _cancellationTokenSource.Token).Token;

            return await Task.Run(() =>
            {
                try
                {
                    return _workItems.Take(combinedToken);
                }
                catch (InvalidOperationException)
                {
                    // Collection was marked as completed
                    return null;
                }
            }, combinedToken);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }

    /// <summary>
    /// Marks the collection as complete for adding and cancels any pending operations
    /// </summary>
    public void CompleteAdding()
    {
        _workItems.CompleteAdding();
        _cancellationTokenSource.Cancel();
    }

    /// <summary>
    /// Gets the current count of pending work items
    /// </summary>
    public int Count => _workItems.Count;

    /// <summary>
    /// Gets whether the queue is marked as complete for adding
    /// </summary>
    public bool IsCompleted => _workItems.IsCompleted;

    public void Dispose()
    {
        if (!_disposed)
        {
            CompleteAdding();
            _workItems.Dispose();
            _cancellationTokenSource.Dispose();
            _disposed = true;
        }
    }
}
