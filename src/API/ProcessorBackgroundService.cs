using System.Threading.Channels;

public class ProcessorBackgroundService(
    ChannelReader<PayloadResult> reader,
    ILogger<ProcessorBackgroundService> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var _ in reader.ReadAllAsync(stoppingToken))
        {
            logger.LogProcessedByWorker();
        }
    }
}
