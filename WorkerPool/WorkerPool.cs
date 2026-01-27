using System.Collections.Concurrent;

namespace WorkerPoolDemo.WorkerPoolDemo;

public class WorkerPool
{
    private readonly ConcurrentQueue<Job> _queue = new();
    private readonly SemaphoreSlim _workers;

    public WorkerPool(int workerCount)
    {
        _workers = new SemaphoreSlim(workerCount, workerCount);
    }

    public async Task<string> EnqueueAsync()
    {
        var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

        _queue.Enqueue(new Job(Guid.NewGuid(), tcs));

        _ = ProcessAsync();

        return await tcs.Task;
    } 

    private async Task ProcessAsync()
    {
        if(!_workers.Wait(0)) return;

        if(!_queue.TryDequeue(out var job))
        {
            _workers.Release();

            return;
        }

        try
        {
            await Task.Delay(100);

            job.Tcs.SetResult("done");
        }
        finally
        {
            _workers.Release();
        }
    }
}
