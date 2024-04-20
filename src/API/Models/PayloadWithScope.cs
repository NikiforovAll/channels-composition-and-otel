using System.Diagnostics;

public record PayloadWithScope<T>(
    T Payload,
    IServiceScope ServiceScope,
    Activity? Activity = default
)
{
    public TService GetRequiredService<TService>()
        where TService : notnull => ServiceScope.ServiceProvider.GetRequiredService<TService>();
}
