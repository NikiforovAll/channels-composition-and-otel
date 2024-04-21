using System.CommandLine;
using System.Threading.Channels;
using Console;
using Open.ChannelExtensions;
using static Console.Extensions.ChannelExtensions;

var gettingStarted = new Command("gt", "Getting started demo");
gettingStarted.SetHandler(async _ =>
{
    // var pipeline = Source(Generate(1, 2, 3));

    // var pipeline = Source(GenerateRange(1..100));

    // var pipeline = Source(GenerateRange(1..10))
    //     .CustomPipe(x => x*x);

    // var pipeline = Source(GenerateRange(1..10))
    //     .CustomPipe(x => (item: x, square: x * x))
    //     .CustomPipe(x => $"{x.item,2}^2 = {x.square,4}");

    var pipeline = Source(GenerateRange(1..10))
        .Pipe(x => (item: x, square: x * x))
        .PipeAsync(
            maxConcurrency: 2,
            async x =>
            {
                await Task.Delay(x.square * 10);

                return x;
            }
        )
        .Pipe(x => $"{x.item,2}^2 = {x.square,4}");

    await pipeline.ForEach(System.Console.WriteLine);
});

var pipeline1 = new Command("p1", "Runs pipeline based on custom implementation");
pipeline1.SetHandler(Pipeline1.RunAsync);

var pipeline2 = new Command("p2", "Runs pipeline based on custom implementation");
pipeline2.SetHandler(Pipeline2.Run);

var rootCommand = new RootCommand() { gettingStarted, pipeline1, pipeline2, };

await rootCommand.InvokeAsync(args);
