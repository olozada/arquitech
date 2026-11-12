namespace Arquitectos.Application;

public static class Paginacion
{
    public const int TamanoDefecto = 50;
    public const int TamanoMaximo = 200;

    public static (int Pagina, int Tamano) Normalizar(int pagina, int tamano)
    {
        if (pagina < 1)
        {
            pagina = 1;
        }

        if (tamano < 1)
        {
            tamano = TamanoDefecto;
        }
        else if (tamano > TamanoMaximo)
        {
            tamano = TamanoMaximo;
        }

        return (pagina, tamano);
    }
}

public sealed record PaginaDto<T>(IReadOnlyList<T> Items, int Total, int Pagina, int Tamano);
