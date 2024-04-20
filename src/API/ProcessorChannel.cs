using System.Diagnostics;
using System.Threading.Channels;
using Microsoft.Extensions.Options;
using Open.ChannelExtensions;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;

public interface IBackgroundProcessor
{
    ValueTask QueueBackgroundWorkItemAsync(
        Payload payload,
        CancellationToken cancellationToken = default
    );
}

public class ProcessorChannel : IBackgroundProcessor
{
    private readonly Channel<Payload> _pipeline;
    private const int MinDelay = 500;
    private const int MaxDelay = 1000;

    public ProcessorChannel(
        IServiceProvider serviceProvider,
        IOptions<ProcessorChannelSettings> processorOptions
    )
    {
        var options = processorOptions.Value;
        _pipeline = Channel.CreateBounded<Payload>(options.Capacity);

        Writer = _pipeline;
        Reader = _pipeline
            .Pipe(
                payload =>
                    InitPipeline(payload, options.UseUnifiedSpanForAllPipelines, serviceProvider),
                options.Capacity
            )
            .PipeAsync(
                maxConcurrency: options.Step1MaxConcurrency,
                Step1,
                capacity: options.Step1Capacity
            )
            .PipeAsync(
                maxConcurrency: options.Step2MaxConcurrency,
                Step2,
                capacity: options.Step2Capacity
            )
            .Pipe(FinishPipeline);
    }

    public async ValueTask QueueBackgroundWorkItemAsync(
        Payload payload,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(payload);
        using Activity? pipelineActivity = Trace(payload);

        await Writer.WriteAsync(payload, cancellationToken);

        Activity.Current?.AddEvent(new ActivityEvent("Item added to the queue"));

        static Activity? Trace(Payload payload)
        {
            var parentActivityContext = Activity.Current?.Context ?? new ActivityContext();
            var pipelineActivity = DiagnosticConfig.Source.StartActivity(
                "Queue item",
                ActivityKind.Producer,
                parentActivityContext
            );

            var tracingMetadata = new TracingMetadata();
            Propagators.DefaultTextMapPropagator.Inject(
                new PropagationContext(parentActivityContext, Baggage.Current),
                tracingMetadata.Metadata,
                TracingMetadata.SetMetadata
            );
            payload.TracingMetadata = tracingMetadata;

            return pipelineActivity;
        }
    }

    private static PayloadWithScope<Payload> InitPipeline(
        Payload payload,
        bool useUnifiedSpanForAllPipelines,
        IServiceProvider serviceProvider
    )
    {
        ServiceMetrics.StartedCounter.Add(1);
        ServiceMetrics.InProgressCounter.Add(1);
        Activity? pipelineActivity = StartPipelineActivity(payload, useUnifiedSpanForAllPipelines);

        var context = new PayloadWithScope<Payload>(
            payload,
            serviceProvider.CreateScope(),
            pipelineActivity
        );

        Trace(pipelineActivity);
        Log(payload, context);

        return context;

        static Activity? StartPipelineActivity(Payload payload, bool useUnifiedSpanForAllPipelines)
        {
            Activity? pipelineActivity;

            if (useUnifiedSpanForAllPipelines)
            {
                pipelineActivity = DiagnosticConfig.Source.StartActivity(
                    "ProcessPipeline",
                    ActivityKind.Consumer,
                    ProcessorBackgroundService.WorkerActivity?.Context ?? new ActivityContext()
                );
            }
            else
            {
                var tracingMetadata = payload.TracingMetadata;
                var activityContext = Propagators.DefaultTextMapPropagator.Extract(
                    default,
                    tracingMetadata.Metadata,
                    TracingMetadata.GetMetadata
                );

                pipelineActivity = DiagnosticConfig.Source.StartActivity(
                    "ProcessPipeline",
                    ActivityKind.Consumer,
                    activityContext.ActivityContext
                );
            }

            pipelineActivity?.SetTag("task.name", payload.Name);

            return pipelineActivity;
        }

        static void Log(Payload payload, PayloadWithScope<Payload> context)
        {
            var logger = context.GetRequiredService<ILogger<ProcessorChannel>>();
            logger.LogPipelineInitiated(payload);
        }

        static void Trace(Activity? pipelineActivity)
        {
            pipelineActivity?.AddEvent(new ActivityEvent("Pipeline initiated"));
        }
    }

    private PayloadResult FinishPipeline(PayloadWithScope<PayloadResult> context)
    {
        Trace(context);
        Log(context);

        context.ServiceScope?.Dispose();

        ServiceMetrics.FinishedCounter.Add(1);
        ServiceMetrics.InProgressCounter.Add(-1);

        return context.Payload;

        static void Trace(PayloadWithScope<PayloadResult> context)
        {
            context.Activity?.AddEvent(new ActivityEvent("Pipeline finished"));
            context.Activity?.Stop();
            context.Activity?.SetStatus(ActivityStatusCode.Ok);
            context.Activity?.Dispose();
        }

        static void Log(PayloadWithScope<PayloadResult> context)
        {
            var logger = context.GetRequiredService<ILogger<ProcessorChannel>>();
            logger.LogPipelineFinished(context.Payload);
        }
    }

    private async ValueTask<PayloadWithScope<Payload2>> Step1(PayloadWithScope<Payload> context)
    {
        ServiceMetrics.InProgressStep1Counter.Add(1);
        using var stepActivity = DiagnosticConfig.Source.StartActivity(
            nameof(Step1),
            ActivityKind.Internal,
            context.Activity.Context
        );
        var timeProvider = context.GetRequiredService<TimeProvider>();

        var delay = Random.Shared.Next(MinDelay, MaxDelay);
        await Task.Delay(delay);

        stepActivity?.SetTag("step.delay", delay);

        ServiceMetrics.InProgressStep1Counter.Add(-1);

        return new(
            new(context.Payload.Name, timeProvider.GetUtcNow(), $"Waited {delay} ms."),
            context.ServiceScope,
            context.Activity
        );
    }

    private async ValueTask<PayloadWithScope<PayloadResult>> Step2(
        PayloadWithScope<Payload2> context
    )
    {
        ServiceMetrics.InProgressStep2Counter.Add(1);

        using var stepActivity = DiagnosticConfig.Source.StartActivity(
            nameof(Step2),
            ActivityKind.Internal,
            context.Activity.Context
        );
        var payload = context.Payload;
        var timeProvider = context.GetRequiredService<TimeProvider>();

        var delay = Random.Shared.Next(MinDelay, MaxDelay);
        await Task.Delay(delay);

        stepActivity?.SetTag("step.delay", delay);

        ServiceMetrics.InProgressStep2Counter.Add(-1);

        return new(
            new(payload.Name, payload.CreatedAt, payload.Message, timeProvider.GetUtcNow()),
            context.ServiceScope,
            context.Activity
        );
    }

    public ChannelWriter<Payload> Writer { get; }
    public ChannelReader<PayloadResult> Reader { get; }
}
