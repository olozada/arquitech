using Microsoft.Extensions.Hosting;

namespace Arquitectos.Infrastructure.Movil;

public sealed class LimpiezaPaquetesMovil : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        ArchivosPaqueteMovil.LimpiarHuerfanos(TimeSpan.FromHours(1));
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
