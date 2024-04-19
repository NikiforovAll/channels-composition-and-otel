using System.Threading.Channels;
using Open.ChannelExtensions;

public class ProcessorChannel
{
    private readonly Channel<Payload> _pipeline;

    public ProcessorChannel(IServiceProvider serviceProvider)
    {
        _pipeline = Channel.CreateBounded<Payload>(10);

        Writer = _pipeline;
        Reader = _pipeline
            .Pipe(payload => InitPipeline(payload, serviceProvider))
            .PipeAsync(Step1)
            .Pipe(Step2)
            .Pipe(FinishPipeline);
    }

    private static PayloadWithScope<Payload> InitPipeline(
        Payload payload,
        IServiceProvider serviceProvider
    )
    {
        var context = new PayloadWithScope<Payload>(payload, serviceProvider.CreateScope());
        var logger = context.GetRequiredService<ILogger<ProcessorChannel>>();

        logger.LogPipelineInitiated(payload);

        return context;
    }

    private PayloadResult FinishPipeline(PayloadWithScope<PayloadResult> context)
    {
        var logger = context.GetRequiredService<ILogger<ProcessorChannel>>();

        logger.LogPipelineFinished(context.Payload);

        context.ServiceScope?.Dispose();

        return context.Payload;
    }

    private async ValueTask<PayloadWithScope<Payload2>> Step1(PayloadWithScope<Payload> context)
    {
        var delay = Random.Shared.Next(500);
        var timeProvider = context.GetRequiredService<TimeProvider>();

        await Task.Delay(delay);

        return new(
            new(context.Payload.Name, timeProvider.GetUtcNow(), $"Waited {delay} ms."),
            context.ServiceScope
        );
    }

    private PayloadWithScope<PayloadResult> Step2(PayloadWithScope<Payload2> context)
    {
        var payload = context.Payload;
        var timeProvider = context.GetRequiredService<TimeProvider>();

        return new(
            new(payload.Name, payload.CreatedAt, payload.Message, timeProvider.GetUtcNow()),
            context.ServiceScope
        );
    }

    public ChannelWriter<Payload> Writer { get; }
    public ChannelReader<PayloadResult> Reader { get; }
}
