public record PayloadWithScope<T>(T Payload, IServiceScope ServiceScope)
{
    public TService GetRequiredService<TService>()
        where TService : notnull => ServiceScope.ServiceProvider.GetRequiredService<TService>();
}
