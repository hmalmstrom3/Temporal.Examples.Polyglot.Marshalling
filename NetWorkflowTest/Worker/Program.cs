using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Temporalio.Client;
using Temporalio.Worker;
using NetWorkflow;
using Temporalio.Converters;

// Configure logging
using var loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
{
    builder
        .AddConsole()
        .SetMinimumLevel(LogLevel.Information);
});

var logger = loggerFactory.CreateLogger<Program>();
logger.LogInformation("Starting Temporal worker...");

// Connect to Temporal server (default localhost:7233)
var client = await TemporalClient.ConnectAsync(
    new TemporalClientConnectOptions("localhost:7233")
    {
        Namespace = "default",
        DataConverter = DataConverter.Default with { PayloadCodec = new NetEncryptionCodec.AesPayloadCodec() },
        LoggerFactory = LoggerFactory.Create(builder =>
            builder.AddSimpleConsole(options => options.TimestampFormat = "[HH:mm:ss] ").
           SetMinimumLevel(LogLevel.Information)),
    });

// Create a cancellation token that is triggered on Ctrl+C
using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (s, e) =>
{
    e.Cancel = true;
    cts.Cancel();
    logger.LogInformation("Shutdown requested, stopping worker...");
};


// Register the worker with the Temporal server
// This worker registers the .NET workflow (Class1) and local .NET activities (SameLangActivities).
// It intentionally does NOT register the Python-based activity (invoked by name "PythonProcessData
TemporalWorkerOptions options = new TemporalWorkerOptions
{
    TaskQueue = "networkflow-task-queue",
    MaxConcurrentActivityTaskPolls = 10,
    MaxConcurrentWorkflowTasks = 10,
    LoggerFactory = loggerFactory
};
options.AddActivity(SameLangActivities.ProcessDataAsync);
options.AddWorkflow<NetToPyWorkflow>();

// Create and run the worker.
// This worker registers the .NET workflow (Class1) and local .NET activities (SameLangActivities).
// It intentionally does NOT register the Python-based activity (invoked by name "PythonProcessDataAsync").
using var worker = new TemporalWorker(
    client,
    options);

var runWorkflow = true;

// Run until cancelled
try
{
    if (runWorkflow)
    {
        try
        {
            var handle = await client.StartWorkflowAsync(
                typeof(NetToPyWorkflow).Name,
                new object[] {
                    new WorkflowParams
                    {
                        InputString = "Hello from C#",
                        Count = 10,
                        IsEnabled = true,
                        Timestamp = DateTime.UtcNow
                    }
                },
                new WorkflowOptions
                {
                    TaskQueue = "networkflow-task-queue",
                    Id = $"net-to-py-workflow-{Guid.CreateVersion7()}",
                });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to start workflow");
            return;
        }
    }
    await worker.ExecuteAsync(cts.Token);
}
catch (OperationCanceledException)
{
    logger.LogInformation("Worker cancelled.");
}
catch (Exception ex)
{
    logger.LogError(ex, "Worker encountered an error.");
}

logger.LogInformation("Worker stopped.");
