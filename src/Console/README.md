# System.Threading.Channels Composition

## Inline pipeline

```csharp
 var pipeline = Source(GenerateRange(1..10))
    .CustomPipe(x => (item: x, square: x * x))
    .CustomPipeAsync(
        maxConcurrency: 2,
        async x =>
        {
            await Task.Delay(x.square * 10);

            return x;
        }
    )
    .CustomPipe(x => $"{x.item}^{x.item} = {x.square}");

await foreach (var item in pipeline.ReadAllAsync())
{
    System.Console.WriteLine(item);
}
```

## Transform Payload

```csharp
var pipeline = Source(Generate(CustomSteps.InitialPayload))
    .PipeAsync<Payload, Payload2>(maxConcurrency: 3, CustomSteps.Step1)
    .Pipe<Payload2, PayloadResult>(CustomSteps.Step2);

await foreach (var item in pipeline.ReadAllAsync())
{
    System.Console.WriteLine(item);
}
```

```csharp
public static class CustomSteps
{
    private const int MinDelay = 500;
    private const int MaxDelay = 1000;

    public static readonly Payload[] InitialPayload =
    [
        new Payload("Task1"),
        new Payload("Task2"),
        new Payload("Task3"),
        new Payload("Task4"),
        new Payload("Task5")
    ];

    public static async ValueTask<Payload2> Step1(Payload payload)
    {
        var timeProvider = TimeProvider.System;

        var delay = Random.Shared.Next(MinDelay, MaxDelay);

        await Task.Delay(delay);

        return new(payload.Name, timeProvider.GetUtcNow(), $"Waited {delay} ms.");
    }

    public static PayloadResult Step2(Payload2 payload)
    {
        var timeProvider = TimeProvider.System;

        return new(payload.Name, payload.CreatedAt, payload.Message, timeProvider.GetUtcNow());
    }
}
```
