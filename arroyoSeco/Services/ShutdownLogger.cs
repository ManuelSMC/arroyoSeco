using Microsoft.Extensions.Hosting;

namespace arroyoSeco.Services;

public class ShutdownLogger : IHostedService
{
    private readonly IHostApplicationLifetime _life;
    public ShutdownLogger(IHostApplicationLifetime life) => _life = life;

    public Task StartAsync(CancellationToken c)
    {
        _life.ApplicationStopping.Register(() => Console.WriteLine("ApplicationStopping"));
        _life.ApplicationStopped.Register(() => Console.WriteLine("ApplicationStopped"));
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken c) => Task.CompletedTask;
}