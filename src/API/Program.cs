using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Threading.Channels;
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry.Resources;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder
    .Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(DiagnosticConfig.ServiceName))
    .WithTracing(tracing =>
    {
        tracing.AddSource(DiagnosticConfig.Source.Name);
    });

builder.Services.AddRequestTimeouts();
builder.Services.AddProblemDetails();
builder.Services.AddHostedService<ProcessorBackgroundService>();
builder.Services.AddSingleton<ProcessorChannel>();
builder.Services.AddSingleton<IBackgroundProcessor, ProcessorChannel>();
builder.Services.AddSingleton<TimeProvider>(_ => TimeProvider.System);

builder.Services.AddSingleton<ChannelReader<PayloadResult>>(sp =>
    sp.GetRequiredService<ProcessorChannel>().Reader
);

builder.Services.Configure<ProcessorChannelSettings>(options =>
{
    options.Capacity = 25;
    options.Step1Capacity = 10;
    options.Step1MaxConcurrency = 5;
    options.Step2Capacity = 10;
    options.Step2MaxConcurrency = 2;

    options.UseUnifiedSpanForAllPipelines = false;
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseRequestTimeouts();

app.MapPost(
        "/start",
        async (
            [FromBody] Payload payload,
            IBackgroundProcessor writer,
            CancellationToken cancellationToken
        ) =>
        {
            await writer.QueueBackgroundWorkItemAsync(payload, cancellationToken);

            return TypedResults.Ok(new { Success = true });
        }
    )
    .WithRequestTimeout(TimeSpan.FromSeconds(5));

app.MapDefaultEndpoints();
app.Run();

public static class DiagnosticConfig
{
    public static string ServiceName = "MyService";
    public static ActivitySource Source = new(ServiceName);
}

public static class ServiceMetrics
{
    private static Meter _meter = new Meter("MyService.Pipelines");

    public static Counter<int> StartedCounter { get; } =
        _meter.CreateCounter<int>("pipelines.started");

    public static UpDownCounter<int> InProgressCounter { get; } =
        _meter.CreateUpDownCounter<int>("pipelines.in-progress");

    public static UpDownCounter<int> InProgressStep1Counter { get; } =
        _meter.CreateUpDownCounter<int>("pipelines.step1.in-progress");

    public static UpDownCounter<int> InProgressStep2Counter { get; } =
        _meter.CreateUpDownCounter<int>("pipelines.step2.in-progress");
    public static Counter<int> FinishedCounter { get; } =
        _meter.CreateCounter<int>("pipelines.finished");
}
