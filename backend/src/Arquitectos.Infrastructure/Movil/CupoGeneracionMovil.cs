using Arquitectos.Application.Movil;
using Microsoft.Extensions.Options;

namespace Arquitectos.Infrastructure.Movil;

public sealed class CupoGeneracionMovil
{
    private readonly SemaphoreSlim _semaforo;

    public CupoGeneracionMovil(IOptions<OpcionesMovil> opciones)
    {
        var cupo = opciones.Value.GeneracionesSimultaneas;
        _semaforo = new SemaphoreSlim(cupo, cupo);
    }

    public Task EsperarAsync(CancellationToken cancellationToken) => _semaforo.WaitAsync(cancellationToken);

    public void Liberar() => _semaforo.Release();
}
