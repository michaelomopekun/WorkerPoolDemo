using System.Collections.Concurrent;
using System.Diagnostics;

namespace WorkerPoolDemo.WorkerPoolDemo;

public class WorkerPool
{
    private readonly ConcurrentQueue<Job> _queue = new();
    private readonly SemaphoreSlim _workers;
    private readonly int _maxWorkers;
    private int _activeWorkers;
    private long _totalProcessed;

    public WorkerPool(int workerCount)
    {
        _maxWorkers = workerCount;
        _workers = new SemaphoreSlim(workerCount, workerCount);
    }

    public async Task<string> EnqueueAsync()
    {
        var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var job = new Job(Guid.NewGuid(), tcs);

        _queue.Enqueue(job);

        Console.WriteLine($"[ENQUEUE] JobId={job.Id:N} | QueueDepth={_queue.Count} | ActiveWorkers={_activeWorkers}/{_maxWorkers}");

        _ = ProcessAsync();

        var sw = Stopwatch.StartNew();
        var result = await tcs.Task;
        sw.Stop();

        Console.WriteLine($"[COMPLETE] JobId={job.Id:N} | TotalLatency={sw.ElapsedMilliseconds}ms");

        return result;
    } 

    private async Task ProcessAsync()
    {
        if (!_workers.Wait(0))
        {
            Console.WriteLine($"[REJECTED] No worker available | ActiveWorkers={_activeWorkers}/{_maxWorkers} | QueueDepth={_queue.Count}");
            return;
        }

        Interlocked.Increment(ref _activeWorkers);

        if (!_queue.TryDequeue(out var job))
        {
            Interlocked.Decrement(ref _activeWorkers);
            _workers.Release();
            Console.WriteLine($"[SKIP] Queue empty after acquiring worker");
            return;
        }

        var sw = Stopwatch.StartNew();

        try
        {
            Console.WriteLine($"[PROCESS] JobId={job.Id:N} | ActiveWorkers={_activeWorkers}/{_maxWorkers} | QueueDepth={_queue.Count}");

            await Task.Delay(100);

            sw.Stop();
            var processed = Interlocked.Increment(ref _totalProcessed);

            Console.WriteLine($"[DONE] JobId={job.Id:N} | ProcessTime={sw.ElapsedMilliseconds}ms | TotalProcessed={processed}");

            job.Tcs.SetResult("done");
        }
        catch (Exception ex)
        {
            sw.Stop();
            Console.WriteLine($"[ERROR] JobId={job.Id:N} | ProcessTime={sw.ElapsedMilliseconds}ms | Error={ex.Message}");
            job.Tcs.SetException(ex);
        }
        finally
        {
            Interlocked.Decrement(ref _activeWorkers);
            _workers.Release();
        }
    }
}
