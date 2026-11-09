namespace Arquitectos.Application.Catalogos;

public sealed class ResultadoCatalogo<T>
{
    public bool Encontrado { get; private init; }

    public bool Duplicado { get; private init; }

    public T? Valor { get; private init; }

    public string? Mensaje { get; private init; }

    public static ResultadoCatalogo<T> Ok(T valor) => new() { Encontrado = true, Valor = valor };

    public static ResultadoCatalogo<T> NoEncontrado(string mensaje) => new() { Mensaje = mensaje };

    public static ResultadoCatalogo<T> Conflicto(string mensaje) => new() { Encontrado = true, Duplicado = true, Mensaje = mensaje };
}
