using System.Threading.Channels;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddProblemDetails();
builder.Services.AddHostedService<ProcessorBackgroundService>();
builder.Services.AddSingleton<ProcessorChannel>();
builder.Services.AddSingleton<ChannelWriter<Payload>>(sp =>
    sp.GetRequiredService<ProcessorChannel>().Writer
);

builder.Services.AddSingleton<ChannelReader<PayloadResult>>(sp =>
    sp.GetRequiredService<ProcessorChannel>().Reader
);

builder.Services.Configure<ProcessorChannelSettings>(options =>
{
    options.Capacity = 5;
});

var app = builder.Build();

app.UseExceptionHandler();

app.MapPost(
    "/start",
    async (
        [FromBody] Payload payload,
        ChannelWriter<Payload> writer,
        CancellationToken cancellationToken
    ) =>
    {
        while (
            await writer.WaitToWriteAsync(cancellationToken)
            && !cancellationToken.IsCancellationRequested
        )
        {
            if (writer.TryWrite(payload))
            {
                return TypedResults.Ok(new { Success = true });
            }
        }

        return TypedResults.Ok(new { Success = false });
    }
);

app.MapDefaultEndpoints();
app.Run();
