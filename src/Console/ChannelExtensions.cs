using System.Threading.Channels;

namespace Console.Extensions;

public static class ChannelExtensions
{
    public static async IAsyncEnumerable<T> Generate<T>(params T[] array)
    {
        foreach (var item in array)
        {
            yield return item;
            await Task.Yield();
        }
    }

    public static async IAsyncEnumerable<int> GenerateRange(Range range)
    {
        int count = range.End.Value - range.Start.Value + 1;
        foreach (var item in Enumerable.Range(range.Start.Value, count))
        {
            yield return item;
            await Task.Yield();
        }
    }

    public static ChannelReader<TOut> Source<TOut>(IAsyncEnumerable<TOut> source)
    {
        var channel = Channel.CreateUnbounded<TOut>();

        Task.Run(async () =>
        {
            await foreach (var item in source)
            {
                await channel.Writer.WriteAsync(item);
            }

            channel.Writer.Complete();
        });

        return channel.Reader;
    }

    public static ChannelReader<TOut> CustomPipe<TRead, TOut>(
        this ChannelReader<TRead> source,
        Func<TRead, TOut> transform
    )
    {
        var channel = Channel.CreateUnbounded<TOut>();

        Task.Run(async () =>
        {
            await foreach (var item in source.ReadAllAsync())
            {
                await channel.Writer.WriteAsync(transform(item));
            }

            channel.Writer.Complete();
        });

        return channel.Reader;
    }

    public static ChannelReader<TOut> CustomPipeAsync<TRead, TOut>(
        this ChannelReader<TRead> source,
        Func<TRead, ValueTask<TOut>> transform
    )
    {
        var channel = Channel.CreateUnbounded<TOut>();

        Task.Run(async () =>
        {
            await foreach (var item in source.ReadAllAsync())
            {
                await channel.Writer.WriteAsync(await transform(item));
            }

            channel.Writer.Complete();
        });

        return channel.Reader;
    }

    public static ChannelReader<TOut> CustomPipeAsync<TRead, TOut>(
        this ChannelReader<TRead> source,
        int maxConcurrency,
        Func<TRead, ValueTask<TOut>> transform
    )
    {
        var bufferChannel = Channel.CreateUnbounded<TOut>();

        var channel = Merge(Split(source, maxConcurrency));

        Task.Run(async () =>
        {
            await foreach (var item in channel.ReadAllAsync())
            {
                await bufferChannel.Writer.WriteAsync(await transform(item));
            }

            bufferChannel.Writer.Complete();
        });

        return bufferChannel.Reader;
    }

    public static async Task ForEach<TRead>(this ChannelReader<TRead> source, Action<TRead> action)
    {
        await foreach (var item in source.ReadAllAsync())
        {
            action(item);
        }
    }

    static ChannelReader<T>[] Split<T>(ChannelReader<T> channel, int n)
    {
        var outputs = Enumerable.Range(0, n).Select(_ => Channel.CreateUnbounded<T>()).ToArray();

        Task.Run(async () =>
        {
            var index = 0;
            await foreach (var item in channel.ReadAllAsync())
            {
                await outputs[index].Writer.WriteAsync(item);
                index = (index + 1) % n;
            }

            foreach (var output in outputs)
                output.Writer.Complete();
        });

        return outputs.Select(output => output.Reader).ToArray();
    }

    static ChannelReader<T> Merge<T>(params ChannelReader<T>[] inputs)
    {
        var output = Channel.CreateUnbounded<T>();

        Task.Run(async () =>
        {
            async Task Redirect(ChannelReader<T> input)
            {
                await foreach (var item in input.ReadAllAsync())
                    await output.Writer.WriteAsync(item);
            }

            await Task.WhenAll(inputs.Select(i => Redirect(i)).ToArray());
            output.Writer.Complete();
        });

        return output;
    }
}

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
