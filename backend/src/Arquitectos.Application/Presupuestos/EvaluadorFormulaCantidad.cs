using System.Globalization;
using System.Text.RegularExpressions;

namespace Arquitectos.Application.Presupuestos;

public sealed record ItemCantidadFormula(int Id, string Codigo, string? FormulaCantidad, decimal CantidadManual);

public sealed record AvisoFormulaCantidad(int? ItemId, string Codigo, string Tipo, string Mensaje);

public sealed record ResultadoFormulaCantidad(IReadOnlyDictionary<int, decimal> Cantidades, IReadOnlyList<AvisoFormulaCantidad> Avisos);

public static partial class EvaluadorFormulaCantidad
{
    private static readonly Regex Referencia = ReferenciaRegex();

    public static ResultadoFormulaCantidad Evaluar(IReadOnlyList<ItemCantidadFormula> items)
    {
        var avisos = new List<AvisoFormulaCantidad>();
        var porCodigo = new Dictionary<string, (int Id, string Codigo)>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in items.Where(fila => !string.IsNullOrWhiteSpace(fila.Codigo)))
        {
            var clave = item.Codigo.Trim();
            if (porCodigo.ContainsKey(clave))
            {
                avisos.Add(new AvisoFormulaCantidad(item.Id, item.Codigo, "CodigoDuplicado", $"El código {clave} está repetido; las fórmulas usan el primero."));
                continue;
            }

            porCodigo[clave] = (item.Id, item.Codigo);
        }

        var cantidades = items.ToDictionary(item => item.Id, item => item.CantidadManual);
        var conFormula = items.Where(item => !string.IsNullOrWhiteSpace(item.FormulaCantidad)).ToList();
        if (conFormula.Count == 0)
        {
            return new ResultadoFormulaCantidad(cantidades, avisos);
        }

        var dependencias = new Dictionary<int, List<int>>();
        foreach (var item in conFormula)
        {
            var lista = new List<int>();
            foreach (Match coincidencia in Referencia.Matches(item.FormulaCantidad!))
            {
                var codigo = coincidencia.Groups[1].Value.Trim();
                if (!porCodigo.TryGetValue(codigo, out var destino))
                {
                    avisos.Add(new AvisoFormulaCantidad(item.Id, item.Codigo, "FormulaReferenciaInvalida", $"La fórmula de {item.Codigo} referencia “{codigo}”, que no existe."));
                    continue;
                }

                if (destino.Id != item.Id)
                {
                    lista.Add(destino.Id);
                }
            }

            dependencias[item.Id] = lista;
        }

        var orden = OrdenTopologico(conFormula.Select(item => item.Id).ToList(), dependencias, avisos, items);
        if (orden is null)
        {
            return new ResultadoFormulaCantidad(cantidades, avisos);
        }

        foreach (var itemId in orden)
        {
            var item = conFormula.First(fila => fila.Id == itemId);
            decimal? Resolver(string codigo)
            {
                if (!porCodigo.TryGetValue(codigo.Trim(), out var destino))
                {
                    return null;
                }

                return cantidades.TryGetValue(destino.Id, out var valor) ? valor : null;
            }

            var valor = EvaluarTexto(item.FormulaCantidad!, codigo =>
                Resolver(codigo) ?? throw new FormatException());
            if (valor is null)
            {
                avisos.Add(new AvisoFormulaCantidad(item.Id, item.Codigo, "FormulaInvalida", $"No se pudo interpretar la fórmula de {item.Codigo}."));
                continue;
            }

            cantidades[item.Id] = valor.Value;
        }

        return new ResultadoFormulaCantidad(cantidades, avisos);
    }

    internal static decimal? EvaluarTexto(string texto, Func<string, decimal> cantidadDe)
    {
        var s = texto.Trim();
        if (s.StartsWith('='))
        {
            s = s[1..];
        }

        s = s.Trim();
        if (s.Length == 0)
        {
            return null;
        }

        return EvaluarExpresion(s, cantidadDe);
    }

    private static List<int>? OrdenTopologico(
        List<int> nodos,
        Dictionary<int, List<int>> dependencias,
        List<AvisoFormulaCantidad> avisos,
        IReadOnlyList<ItemCantidadFormula> items)
    {
        var entrada = nodos.ToDictionary(id => id, id => dependencias.TryGetValue(id, out var lista) ? lista.Count : 0);
        var salida = new Dictionary<int, List<int>>();
        foreach (var (id, deps) in dependencias)
        {
            foreach (var dep in deps)
            {
                if (!salida.TryGetValue(dep, out var hijos))
                {
                    hijos = [];
                    salida[dep] = hijos;
                }

                hijos.Add(id);
            }
        }

        var cola = new Queue<int>(entrada.Where(par => par.Value == 0).Select(par => par.Key));
        var orden = new List<int>();
        while (cola.Count > 0)
        {
            var actual = cola.Dequeue();
            orden.Add(actual);
            if (!salida.TryGetValue(actual, out var hijos))
            {
                continue;
            }

            foreach (var hijo in hijos)
            {
                entrada[hijo]--;
                if (entrada[hijo] == 0)
                {
                    cola.Enqueue(hijo);
                }
            }
        }

        if (orden.Count != nodos.Count)
        {
            var ciclo = items.FirstOrDefault(item => !orden.Contains(item.Id) && dependencias.ContainsKey(item.Id));
            var codigo = ciclo?.Codigo ?? "?";
            avisos.Add(new AvisoFormulaCantidad(ciclo?.Id, codigo, "FormulaCiclica", $"Hay una referencia circular en las cantidades (p. ej. {codigo})."));
            return null;
        }

        return orden;
    }

    private static decimal? EvaluarExpresion(string expresion, Func<string, decimal> cantidadDe)
    {
        var tokens = Tokenizar(expresion, cantidadDe);
        if (tokens.Count == 0)
        {
            return null;
        }

        var indice = 0;
        try
        {
            var valor = ParsearSuma(tokens, ref indice, cantidadDe);
            return indice == tokens.Count ? valor : null;
        }
        catch
        {
            return null;
        }
    }

    private static List<object> Tokenizar(string expresion, Func<string, decimal> cantidadDe)
    {
        var tokens = new List<object>();
        for (var i = 0; i < expresion.Length;)
        {
            var c = expresion[i];
            if (char.IsWhiteSpace(c))
            {
                i++;
                continue;
            }

            if (c is '+' or '-' or '*' or '/' or '(' or ')')
            {
                tokens.Add(c);
                i++;
                continue;
            }

            if (c == '[')
            {
                var fin = expresion.IndexOf(']', i + 1);
                if (fin < 0)
                {
                    throw new FormatException();
                }

                var codigo = expresion[(i + 1)..fin].Trim();
                var valor = cantidadDe(codigo);
                if (double.IsNaN((double)valor))
                {
                    throw new FormatException();
                }

                tokens.Add(valor);
                i = fin + 1;
                continue;
            }

            var inicio = i;
            while (i < expresion.Length && (char.IsDigit(expresion[i]) || expresion[i] is ',' or '.'))
            {
                i++;
            }

            var texto = expresion[inicio..i];
            if (!TryNumero(texto, out var numero))
            {
                throw new FormatException();
            }

            tokens.Add(numero);
        }

        return tokens;
    }

    private static decimal ParsearSuma(List<object> tokens, ref int indice, Func<string, decimal> cantidadDe)
    {
        var valor = ParsearProducto(tokens, ref indice, cantidadDe);
        while (indice < tokens.Count && tokens[indice] is '+' or '-')
        {
            var op = (char)tokens[indice++];
            var derecho = ParsearProducto(tokens, ref indice, cantidadDe);
            valor = op == '+' ? valor + derecho : valor - derecho;
        }

        return valor;
    }

    private static decimal ParsearProducto(List<object> tokens, ref int indice, Func<string, decimal> cantidadDe)
    {
        var valor = ParsearUnario(tokens, ref indice, cantidadDe);
        while (indice < tokens.Count && tokens[indice] is '*' or '/')
        {
            var op = (char)tokens[indice++];
            var derecho = ParsearUnario(tokens, ref indice, cantidadDe);
            valor = op == '*' ? valor * derecho : valor / derecho;
        }

        return valor;
    }

    private static decimal ParsearUnario(List<object> tokens, ref int indice, Func<string, decimal> cantidadDe)
    {
        if (indice < tokens.Count && tokens[indice] is '-' or '+')
        {
            var op = (char)tokens[indice++];
            var valor = ParsearUnario(tokens, ref indice, cantidadDe);
            return op == '-' ? -valor : valor;
        }

        if (indice < tokens.Count && tokens[indice] is '(')
        {
            indice++;
            var valor = ParsearSuma(tokens, ref indice, cantidadDe);
            if (indice >= tokens.Count || tokens[indice] is not ')')
            {
                throw new FormatException();
            }

            indice++;
            return valor;
        }

        if (indice < tokens.Count && tokens[indice] is decimal numero)
        {
            indice++;
            return numero;
        }

        throw new FormatException();
    }

    private static bool TryNumero(string texto, out decimal valor)
    {
        valor = 0;
        var normalizado = texto;
        if (texto.Contains(','))
        {
            normalizado = texto.Replace(".", "").Replace(',', '.');
        }
        else if (texto.Count(c => c == '.') > 1)
        {
            normalizado = texto.Replace(".", "");
        }

        return decimal.TryParse(normalizado, NumberStyles.Number, CultureInfo.InvariantCulture, out valor);
    }

    [GeneratedRegex(@"\[([^\]]+)\]", RegexOptions.CultureInvariant)]
    private static partial Regex ReferenciaRegex();
}