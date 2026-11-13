using System.Text.Json;
using Arquitectos.Application.Auditoria;
using Arquitectos.Application.Presupuestos;
using Arquitectos.Application.Usuarios;
using Arquitectos.Domain.Entidades;
using Arquitectos.Infrastructure.Auditoria;
using Arquitectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Arquitectos.Infrastructure.Presupuestos;

public sealed partial class PresupuestoServicio
{
    public async Task<ResultadoOperacion<PresupuestoDetalleDto>> CrearAsync(
        PresupuestoSolicitud solicitud,
        int usuarioId,
        CancellationToken cancellationToken)
    {
        if (!await EsAdministradorAsync(usuarioId, cancellationToken))
        {
            return ResultadoOperacion<PresupuestoDetalleDto>.Prohibido("Solo el administrador puede crear presupuestos.");
        }

        if (!await _context.Construcciones.AnyAsync(obra => obra.Id == solicitud.ConstruccionId && obra.Activo, cancellationToken))
        {
            return ResultadoOperacion<PresupuestoDetalleDto>.Validacion("La construcción no existe.");
        }

        var version = await _context.Presupuestos
            .Where(item => item.ConstruccionId == solicitud.ConstruccionId)
            .Select(item => (int?)item.Version)
            .MaxAsync(cancellationToken);
        var presupuesto = new Presupuesto
        {
            ConstruccionId = solicitud.ConstruccionId,
            Nombre = TextoPresupuesto.Recortar(string.IsNullOrWhiteSpace(solicitud.Nombre) ? "Presupuesto" : solicitud.Nombre, 200),
            Version = (version ?? 0) + 1,
            Estado = EstadoPresupuesto.Borrador,
            Fecha = solicitud.Fecha == default ? DateOnly.FromDateTime(DateTime.UtcNow) : solicitud.Fecha
        };
        CopiarEncabezado(presupuesto, solicitud);
        var error = await CopiarArbolAsync(presupuesto, solicitud, cancellationToken);
        if (error is not null)
        {
            return ResultadoOperacion<PresupuestoDetalleDto>.Validacion(error);
        }

        await AsegurarConceptosAsync(presupuesto, cancellationToken);
        _context.Presupuestos.Add(presupuesto);
        await AplicarCalculoAsync(presupuesto, true, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return await ObtenerAsync(presupuesto.Id, usuarioId, cancellationToken);
    }


    public async Task<ResultadoOperacion<InformeImportacionDto>> ImportarAsync(
        Stream archivo,
        int construccionId,
        int usuarioId,
        CancellationToken cancellationToken)
    {
        if (!await EsAdministradorAsync(usuarioId, cancellationToken))
        {
            return ResultadoOperacion<InformeImportacionDto>.Prohibido("Solo el administrador puede importar.");
        }

        var invalido = ValidadorLibroExcel.RevisarContenido(archivo);
        if (invalido is not null)
        {
            return ResultadoOperacion<InformeImportacionDto>.Validacion(invalido);
        }

        if (!await _context.Construcciones.AnyAsync(obra => obra.Id == construccionId && obra.Activo, cancellationToken))
        {
            return ResultadoOperacion<InformeImportacionDto>.Validacion("La construcción no existe.");
        }

        ModeloImportado modelo;
        using var tiempo = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        tiempo.CancelAfter(ValidadorLibroExcel.TiempoMaximo);
        try
        {
            var filas = _opciones.FilasMaximas > 0 ? _opciones.FilasMaximas : 20_000;
            modelo = LectorExcel.Leer(archivo, tiempo.Token, filas, ValidadorLibroExcel.HojasMaximas);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return ResultadoOperacion<InformeImportacionDto>.Validacion("La importación superó el tiempo permitido.");
        }
        catch (Exception excepcion) when (excepcion is InvalidOperationException or IOException or InvalidDataException)
        {
            return ResultadoOperacion<InformeImportacionDto>.Validacion(excepcion.Message);
        }

        var informe = await new ImportadorPresupuesto(_context).GuardarAsync(modelo, construccionId, cancellationToken);
        await _auditoria.RegistrarAsync(
            "Presupuesto",
            informe.PresupuestoId.ToString(),
            AccionAuditoria.Importar,
            usuarioId,
            null,
            cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return ResultadoOperacion<InformeImportacionDto>.Ok(informe);
    }


    private async Task<string?> CopiarArbolAsync(Presupuesto presupuesto, PresupuestoSolicitud solicitud, CancellationToken cancellationToken)
    {
        var error = Codigos(solicitud);
        if (error is not null)
        {
            return error;
        }

        var ordenCapitulo = 1;
        foreach (var capitulo in solicitud.Capitulos)
        {
            var nuevo = new PresupuestoCapitulo
            {
                Numero = capitulo.Numero,
                Nombre = TextoPresupuesto.Recortar(capitulo.Nombre, 200),
                Orden = ordenCapitulo++
            };
            var orden = 1;
            foreach (var item in capitulo.Items)
            {
                nuevo.Items.Add(CrearItem(item, orden++));
            }

            presupuesto.Capitulos.Add(nuevo);
        }

        return await Task.FromResult(CicloCantidades(presupuesto));
    }

}
