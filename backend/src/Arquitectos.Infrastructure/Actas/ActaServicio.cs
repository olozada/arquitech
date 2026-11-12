using System.Globalization;
using System.Linq.Expressions;
using Arquitectos.Application.Actas;
using Arquitectos.Application.Auditoria;
using Arquitectos.Application.Usuarios;
using Arquitectos.Domain.Entidades;
using Arquitectos.Infrastructure.Auditoria;
using Arquitectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Arquitectos.Infrastructure.Actas;

public sealed partial class ActaServicio : IActaServicio
{
    private readonly ArquitectosDbContext _context;
    private readonly IAuditoria _auditoria;

    public ActaServicio(ArquitectosDbContext context, IAuditoria? auditoria = null)
    {
        _context = context;
        _auditoria = auditoria ?? SinAuditoria.Instancia;
    }
}
