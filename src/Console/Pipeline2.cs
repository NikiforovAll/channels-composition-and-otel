namespace Console;

using Console.Extensions;
using Open.ChannelExtensions;
using static Console.Extensions.ChannelExtensions;

public static class Pipeline2
{
    public static async Task Run()
    {
        var pipeline = Source(Generate(CustomSteps.InitialPayload))
            .PipeAsync<Payload, Payload2>(maxConcurrency: 3, CustomSteps.Step1)
            .Pipe<Payload2, PayloadResult>(CustomSteps.Step2);

        await foreach (var item in pipeline.ReadAllAsync())
        {
            System.Console.WriteLine(item);
        }
    }
}
