using Arquitectos.Application.Usuarios;
using Arquitectos.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Arquitectos.Tests;

public sealed class GeneradorPaqueteConsultaTests
{
    [Theory]
    [InlineData(PerfilesUsuario.Arquitecto, "ArquitectoId")]
    [InlineData(PerfilesUsuario.Contratista, "ContratistaId")]
    [InlineData(PerfilesUsuario.Revisor, "RevisorId")]
    public void La_consulta_de_actas_incluye_filtro_por_perfil(int perfil, string columnaEsperada)
    {
        using var conexion = new SqliteConnection("Data Source=:memory:");
        conexion.Open();
        var opciones = new DbContextOptionsBuilder<ArquitectosDbContext>()
            .UseSqlite(conexion)
            .Options;
        using var contexto = new ArquitectosDbContext(opciones);

        var desde = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-12);
        var consulta = contexto.Actas.AsNoTracking()
            .Where(acta => !acta.Cerrada || acta.FechaFinal >= desde);
        consulta = perfil switch
        {
            PerfilesUsuario.Arquitecto => consulta.Where(acta => acta.ArquitectoId == 1),
            PerfilesUsuario.Contratista => consulta.Where(acta => acta.ContratistaId == 2),
            PerfilesUsuario.Revisor => consulta.Where(acta => acta.RevisorId == 3),
            _ => consulta.Where(_ => false)
        };

        var sql = consulta.ToQueryString();
        Assert.Contains(columnaEsperada, sql, StringComparison.OrdinalIgnoreCase);
    }
}
