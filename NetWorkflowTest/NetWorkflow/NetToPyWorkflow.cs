using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Temporalio.Workflows;
using Temporalio.Activities;
using System.Diagnostics;

namespace NetWorkflow;

[Workflow]
public class NetToPyWorkflow
{

    [WorkflowRun]
    public async Task<string> WorkflowRun(WorkflowParams p)
    {
        var logger = Workflow.Logger;
        logger.LogInformation("Testing polyglot workflow marshalling...");
        ActivityInputData ourInput = new ActivityInputData
        {
            Input1 = "Test Input",
            isEnabled = true,
            Count = 42,
            ArrItems = new[] { "Item1", "Item2" },
            ListItems = new List<string> { "ListItem1", "ListItem2" },
            DictItems = new Dictionary<string, string> { { "Key1", "Value1" }, { "Key2", "Value2" } },
            Timestamp = DateTime.UtcNow
        };

        var result = await Workflow.ExecuteActivityAsync(
            () => SameLangActivities.ProcessDataAsync(ourInput),
            new ActivityOptions
            {
                ScheduleToCloseTimeout = TimeSpan.FromMinutes(5),
                StartToCloseTimeout = TimeSpan.FromMinutes(1)
            });
        logger.LogInformation("Activity completed: {Result}", result);

        // Calling a Python-based Activity from a C# workflow
        var pyResult = await Workflow.ExecuteActivityAsync<ReturnData>("PythonProcessDataAsync",
            new object[] { ourInput },
            new ActivityOptions
            {
                TaskQueue = "python-task-queue",
                ScheduleToCloseTimeout = TimeSpan.FromMinutes(5),
                StartToCloseTimeout = TimeSpan.FromMinutes(1)
            });
        logger.LogInformation("Python activity completed: {Result}", pyResult.Result);

        return result;
    }


}

public class WorkflowParams
{
    public string InputString { get; set; } = string.Empty;
    public int Count { get; set; } = 0;
    public bool IsEnabled { get; set; } = false;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class SameLangActivities
{
    [Activity]
    public static async Task<string> ProcessDataAsync(ActivityInputData data)
    {
        // Simulate some work
        await Task.Delay(TimeSpan.FromSeconds(1));
        return $"Processed: {data.Input1}";
    }
}

public class ActivityInputData
{
    public string Input1 { get; set; } = string.Empty;
    public Boolean isEnabled { get; set; } = false;
    public int Count { get; set; } = 0;
    public string[] ArrItems { get; set; } = Array.Empty<string>();
    public List<string> ListItems { get; set; } = new List<string>();
    public Dictionary<string, string> DictItems { get; set; } = new Dictionary<string, string>();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class ReturnData
{
    public string Result { get; set; } = string.Empty;
    public bool Success { get; set; } = false;
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}

