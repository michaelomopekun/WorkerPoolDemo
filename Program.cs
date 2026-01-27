using WorkerPoolDemo.WorkerPoolDemo;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var workerPool = new WorkerPool(workerCount: 5);

app.MapPost("/work", async () =>
{
    return await workerPool.EnqueueAsync();
});

app.Run();