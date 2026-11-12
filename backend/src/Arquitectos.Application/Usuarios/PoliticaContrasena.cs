namespace Arquitectos.Application.Usuarios;

public static class PoliticaContrasena
{
    public static string? Validar(string contrasena, string? nombreUsuario)
    {
        if (contrasena.Length < MensajesUsuario.LongitudMinima)
        {
            return MensajesUsuario.PasswordCorta;
        }

        if (!contrasena.Any(char.IsLetter) || !contrasena.Any(char.IsDigit))
        {
            return MensajesUsuario.PasswordComposicion;
        }

        if (!string.IsNullOrEmpty(nombreUsuario) &&
            string.Equals(contrasena, nombreUsuario, StringComparison.OrdinalIgnoreCase))
        {
            return MensajesUsuario.PasswordIgualUsuario;
        }

        if (ContrasenasComunes.Contiene(contrasena))
        {
            return MensajesUsuario.PasswordComun;
        }

        return null;
    }

    private static class ContrasenasComunes
    {
        private static readonly HashSet<string> Lista = Cargar();

        public static bool Contiene(string contrasena) =>
            Lista.Contains(contrasena.ToLowerInvariant());

        private static HashSet<string> Cargar()
        {
            var ensamblado = typeof(PoliticaContrasena).Assembly;
            var nombre = ensamblado.GetManifestResourceNames()
                .Single(recurso => recurso.EndsWith("contrasenas-comunes.txt", StringComparison.Ordinal));
            using var flujo = ensamblado.GetManifestResourceStream(nombre)!;
            using var lector = new StreamReader(flujo);
            var claves = new HashSet<string>(StringComparer.Ordinal);
            while (lector.ReadLine() is { } linea)
            {
                var texto = linea.Trim().ToLowerInvariant();
                if (texto.Length > 0)
                {
                    claves.Add(texto);
                }
            }

            return claves;
        }
    }
}
