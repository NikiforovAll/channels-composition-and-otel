using System.Diagnostics;
using System.Threading.Channels;

public class ProcessorBackgroundService(
    ChannelReader<PayloadResult> reader,
    ILogger<ProcessorBackgroundService> logger
) : BackgroundService
{
    // NOTE, don't use in production scenarios, used for visualization/demo purposes
    public static Activity? WorkerActivity { get; private set; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var activity = DiagnosticConfig.Source.StartActivity("BackgroundWorker");
        Marker();

        await foreach (var item in reader.ReadAllAsync(stoppingToken))
        {
            logger.LogProcessedByWorker();

            activity?.AddEvent(new ActivityEvent($"Processed - {item.Name}"));
        }
    }

    private static void Marker()
    {
        Activity.Current = null;

        using var activityMarker = DiagnosticConfig.Source.StartActivity(
            $"BackgroundWorker.Marker",
            ActivityKind.Server,
            parentId: null
        );

        WorkerActivity = activityMarker;
    }
}
