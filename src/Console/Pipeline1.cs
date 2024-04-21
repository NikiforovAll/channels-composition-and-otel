using Console.Extensions;
using static Console.Extensions.ChannelExtensions;

namespace Console;

public static class Pipeline1
{
    public static async Task RunAsync()
    {
        var pipeline = Source(Generate(CustomSteps.InitialPayload))
            .CustomPipeAsync<Payload, Payload2>(maxConcurrency: 3, CustomSteps.Step1)
            .CustomPipe<Payload2, PayloadResult>(CustomSteps.Step2);

        await foreach (var item in pipeline.ReadAllAsync())
        {
            System.Console.WriteLine(item);
        }
    }
}
