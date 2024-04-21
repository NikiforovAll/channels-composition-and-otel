using System.CommandLine;
using Console;
using static Console.Extensions.ChannelExtensions;

var gettingStarted = new Command("gt", "Getting started demo");
gettingStarted.SetHandler(async _ =>
{
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
});

var pipeline1 = new Command("p1", "Runs pipeline based on custom implementation");
pipeline1.SetHandler(Pipeline1.RunAsync);

var pipeline2 = new Command("p2", "Runs pipeline based on custom implementation");
pipeline2.SetHandler(Pipeline2.Run);

var rootCommand = new RootCommand() { gettingStarted, pipeline1, pipeline2, };

await rootCommand.InvokeAsync(args);
