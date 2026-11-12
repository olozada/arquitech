namespace Arquitectos.Application.ProveedorMateriales;

public sealed record MaterialAgregadoDto(int Id, string Descripcion, decimal Precio);

public sealed record MaterialDisponibleDto(int Id, string Descripcion);

public sealed class AgregarMaterialSolicitud
{
    public int InsumoId { get; set; }

    public decimal Precio { get; set; }
}

public enum EstadoProveedorMaterial
{
    Ok,
    NoEncontrado,
    Duplicado,
    Invalido
}

public sealed class ResultadoProveedorMaterial<T>
{
    public EstadoProveedorMaterial Estado { get; private init; }

    public string? Mensaje { get; private init; }

    public T? Valor { get; private init; }

    public static ResultadoProveedorMaterial<T> Ok(T valor) =>
        new() { Estado = EstadoProveedorMaterial.Ok, Valor = valor };

    public static ResultadoProveedorMaterial<T> NoEncontrado(string mensaje) =>
        new() { Estado = EstadoProveedorMaterial.NoEncontrado, Mensaje = mensaje };

    public static ResultadoProveedorMaterial<T> Duplicado(string mensaje) =>
        new() { Estado = EstadoProveedorMaterial.Duplicado, Mensaje = mensaje };

    public static ResultadoProveedorMaterial<T> Invalido(string mensaje) =>
        new() { Estado = EstadoProveedorMaterial.Invalido, Mensaje = mensaje };
}
