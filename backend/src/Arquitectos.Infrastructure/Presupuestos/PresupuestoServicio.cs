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

public sealed partial class PresupuestoServicio : IPresupuestoServicio
{
    private readonly ArquitectosDbContext _context;
    private readonly MotorCostos _motor;
    private readonly OpcionesPresupuesto _opciones;
    private readonly IAuditoria _auditoria;

    public PresupuestoServicio(
        ArquitectosDbContext context,
        MotorCostos motor,
        IOptions<OpcionesPresupuesto>? opciones = null,
        IAuditoria? auditoria = null)
    {
        _context = context;
        _motor = motor;
        _opciones = opciones?.Value ?? new OpcionesPresupuesto();
        _auditoria = auditoria ?? SinAuditoria.Instancia;
    }
}
