using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Arquitectos.Application.Auth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Arquitectos.Infrastructure.Seguridad;

public sealed class EmisorJwt
{
    private readonly OpcionesJwt _opciones;

    public EmisorJwt(IOptions<OpcionesJwt> opciones)
    {
        _opciones = opciones.Value;
    }

    public int Minutos => _opciones.ExpirationMinutes > 0 ? _opciones.ExpirationMinutes : 20;

    public string Emitir(int usuarioId, string nombreUsuario, string nombre, int tipoUsuarioId, string tipoUsuario, long sesionId)
    {
        if (string.IsNullOrWhiteSpace(_opciones.Key))
        {
            throw new InvalidOperationException("Jwt:Key no está configurada.");
        }

        var minutos = _opciones.ExpirationMinutes > 0 ? _opciones.ExpirationMinutes : 20;
        var credenciales = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opciones.Key)) { KeyId = "arquitectos" },
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _opciones.Issuer,
            audience: _opciones.Audience,
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, usuarioId.ToString()),
                new Claim(ClaimsArquitectos.Usuario, nombreUsuario),
                new Claim(ClaimsArquitectos.Nombre, nombre),
                new Claim(ClaimsArquitectos.TipoUsuarioId, tipoUsuarioId.ToString()),
                new Claim(ClaimsArquitectos.TipoUsuario, tipoUsuario),
                new Claim(ClaimsArquitectos.Sesion, sesionId.ToString())
            ],
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(minutos),
            signingCredentials: credenciales);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
