public static partial class LoggerExtensions
{
    [LoggerMessage(1001, LogLevel.Information, "Initiated pipeline for Payload - {Payload}")]
    public static partial void LogPipelineInitiated(
        this ILogger logger,
        [LogProperties] Payload payload
    );

    [LoggerMessage(1002, LogLevel.Information, "Finished with Payload - {Payload}")]
    public static partial void LogPipelineFinished(
        this ILogger logger,
        [LogProperties] PayloadResult payload
    );

    [LoggerMessage(1003, LogLevel.Debug, "Processed by BackgroundWorker")]
    public static partial void LogProcessedByWorker(this ILogger logger);
}
