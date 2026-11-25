using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;

if (args.Length < 2)
{
    Console.Error.WriteLine("Uso: ActualizarClavesUsuarios <connectionString> <nuevaClave>");
    return 1;
}

var connectionString = args[0];
var nuevaClave = args[1];
var hasher = new PasswordHasher<string>();

await using var conexion = new SqlConnection(connectionString);
await conexion.OpenAsync();

var usuarios = new List<(int Id, string Usuario)>();
await using (var leer = new SqlCommand(
                 "SELECT Id, Usuario FROM Usuarios",
                 conexion))
await using (var reader = await leer.ExecuteReaderAsync())
{
    while (await reader.ReadAsync())
    {
        usuarios.Add((reader.GetInt32(0), reader.GetString(1)));
    }
}

var actualizados = 0;
foreach (var (id, nombreUsuario) in usuarios)
{
    var hash = hasher.HashPassword(nombreUsuario, nuevaClave);
    await using var actualizar = new SqlCommand(
        """
        UPDATE Usuarios
        SET PasswordHash = @hash,
            DebeCambiarContrasena = 0,
            IntentosFallidos = 0,
            BloqueadoHasta = NULL
        WHERE Id = @id
        """,
        conexion);
    actualizar.Parameters.AddWithValue("@hash", hash);
    actualizar.Parameters.AddWithValue("@id", id);
    actualizados += await actualizar.ExecuteNonQueryAsync();
    Console.WriteLine($"Id={id} Usuario={nombreUsuario}");
}

Console.WriteLine($"Total actualizados: {actualizados}");
return 0;