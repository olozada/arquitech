using Arquitectos.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Arquitectos.Infrastructure.Persistence;

/// <summary>
/// Datos de demostración con los mismos casos que datos_demo/generar_datos_demo.py.
/// </summary>
public static class SemillaDemo
{
    public static async Task<string> AplicarAsync(
        ArquitectosDbContext context,
        CancellationToken cancellationToken = default,
        DateOnly? hoy = null)
    {
        if (await context.Actas.AnyAsync(cancellationToken))
        {
            return "La base ya tiene actas. No se insertó la demostración.";
        }

        var fecha = hoy ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var (lista, actas) = GeneradorDemo.Generar(fecha);
        var arquitectos = GeneradorDemo.Arquitectos.Select(persona => new Arquitecto
        {
            Nombre = persona.Nombre,
            Cedula = persona.Documento,
        }).ToList();
        var revisores = GeneradorDemo.Revisores.Select(persona => new Revisor
        {
            Nombre = persona.Nombre,
            Cedula = persona.Documento,
        }).ToList();
        var contratistas = GeneradorDemo.Contratistas.Select(persona => new Contratista
        {
            Nombre = persona.Nombre,
            Cedula = persona.Documento,
        }).ToList();
        var proveedores = GeneradorDemo.Proveedores.Select(persona => new Proveedor
        {
            Nombre = persona.Nombre,
            Nit = persona.Documento,
        }).ToList();
        var unidades = GeneradorDemo.Unidades.Select(unidad => new UnidadMedida
        {
            Nombre = unidad.Codigo,
            Descripcion = unidad.Descripcion,
        }).ToList();
        context.AddRange(arquitectos);
        context.AddRange(revisores);
        context.AddRange(contratistas);
        context.AddRange(proveedores);
        context.AddRange(unidades);
        await context.SaveChangesAsync(cancellationToken);

        var unidadPorNombre = unidades.ToDictionary(unidad => unidad.Nombre, unidad => unidad.Id);
        var insumos = GeneradorDemo.Materiales.Select((material, indice) => new Insumo
        {
            Codigo = $"MAT-{(indice + 1):000}",
            Descripcion = material.Nombre,
            Tipo = TipoInsumo.Material,
            UnidadCompra = material.Unidad,
            UnidadUso = material.Unidad,
            PrecioBase = (decimal)material.Precio,
            CantidadPorUnidad = 1,
            FechaPrecio = DateTime.UtcNow,
        }).ToList();
        context.Insumos.AddRange(insumos);
        var conceptos = GeneradorDemo.Conceptos.Select((concepto, indice) => new Concepto
        {
            Codigo = $"CON-{(indice + 1):000}",
            Descripcion = concepto.Nombre,
            Unidad = concepto.Unidad,
        }).ToList();
        context.Conceptos.AddRange(conceptos);
        var obras = GeneradorDemo.Obras.Select(obra => new Construccion
        {
            Descripcion = obra.Nombre,
            Propietario = obra.Propietario,
            Activo = obra.Fin is not DateOnly fin || fin >= fecha.AddDays(-90),
        }).ToList();
        context.Construcciones.AddRange(obras);
        await context.SaveChangesAsync(cancellationToken);

        var listas = lista.Select(precio => new ProveedorMaterial
        {
            ProveedorId = proveedores[precio.Key.Proveedor - 1].Id,
            InsumoId = insumos[precio.Key.Material - 1].Id,
            Precio = precio.Value,
        }).ToList();

        context.ProveedorMateriales.AddRange(listas);

        int? usuarioId = await context.Usuarios
            .OrderBy(usuario => usuario.Id)
            .Select(usuario => (int?)usuario.Id)
            .FirstOrDefaultAsync(cancellationToken);
        foreach (var plan in actas)
        {
            var acta = new Acta
            {
                ArquitectoId = arquitectos[plan.Arquitecto - 1].Id,
                RevisorId = revisores[plan.Revisor - 1].Id,
                ContratistaId = contratistas[plan.Contratista - 1].Id,
                FechaInicial = plan.Inicio,
                FechaFinal = plan.Fin,
                FechaRegistro = plan.Registro,
                UsuarioRegistroId = usuarioId,
                Observacion = plan.Observacion,
                Cerrada = true,
                Detalles = plan.Lineas.Select(linea => new ActaDetalle
                {
                    ConstruccionId = obras[linea.Construccion - 1].Id,
                    ConceptoId = conceptos[linea.Concepto - 1].Id,
                    UnidadMedidaId = unidadPorNombre[linea.Unidad],
                    InsumoId = linea.Material is int material ? insumos[material - 1].Id : null,
                    Cantidad = linea.Cantidad,
                    Precio = linea.Precio,
                    Descripcion = linea.Descripcion,
                    Aprobado = true,
                    TipoUsuarioId = SemillaInicial.TipoArquitectoId,
                }).ToList(),
            };
            context.Actas.Add(acta);
        }

        await context.SaveChangesAsync(cancellationToken);
        return $"Demostración al {fecha:yyyy-MM-dd}: {actas.Count} actas.";
    }
}
