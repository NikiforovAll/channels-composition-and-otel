# System.Threading.Channels Composition and OpenTelemetry

The project demonstrates how to build an in-memory pipeline that allows to offload long-running tasks. As result, we have an improve user experience because the result is returned as soon as a task is scheduled.

```csharp
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
);
```

```csharp
public class ProcessorChannel : IBackgroundProcessor
{
    public ProcessorChannel()
    {
        var options = processorOptions.Value;
        _pipeline = Channel.CreateBounded<Payload>(10);

        Writer = _pipeline;
        Reader = _pipeline
            .Pipe(InitPipeline, capacity: 10)
            .PipeAsync(
                maxConcurrency: 5,
                Step1,
                capacity: 10
            )
            .PipeAsync(
                maxConcurrency: 2,
                Step2,
                capacity: 10
            )
            .Pipe(FinishPipeline);
    }
}
```

![producer-consumer](/assets/producer-consumer.png)

## Demo

```bash
dotnet run --project ./src/AppHost
```

```bash
./scripts/bombardier.sh 100
```

`ProcessorChannelSettings.UseUnifiedSpanForAllPipelines=false`

![pipeline-trace](/assets/pipeline-trace.png)

`ProcessorChannelSettings.UseUnifiedSpanForAllPipelines=true`

![batch-trace](/assets/batch-trace.png)


```bash
dotnet-counters monitor -n API --counters MyService.Pipelines
```

![metrics](/assets/metrics.png)

## Reference

* <https://devblogs.microsoft.com/dotnet/an-introduction-to-system-threading-channels/>
* <https://github.com/Open-NET-Libraries/Open.ChannelExtensions>
* <https://blog.maartenballiauw.be/post/2020/08/26/producer-consumer-pipelines-with-system-threading-channels.html>
* <https://deniskyashif.com/2019/12/08/csharp-channels-part-1/>
* <https://github.com/martinjt/dotnet-background-otel/tree/main>
* <https://learn.microsoft.com/en-us/dotnet/core/diagnostics/distributed-tracing-instrumentation-walkthroughs>
* <https://opentelemetry.io/docs/languages/net/instrumentation/>
* <https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/examples/MicroserviceExample>
* <https://learn.microsoft.com/en-us/dotnet/core/diagnostics/metrics-instrumentation>
