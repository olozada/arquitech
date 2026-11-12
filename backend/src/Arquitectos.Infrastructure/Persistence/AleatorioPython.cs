namespace Arquitectos.Infrastructure.Persistence;

/// <summary>
/// Misma secuencia que random.Random de Python 3 al sembrar con un entero.
/// </summary>
internal sealed class AleatorioPython
{
    private const int N = 624;
    private const int M = 397;
    private const uint MatrixA = 0x9908b0df;
    private const uint Upper = 0x80000000;
    private const uint Lower = 0x7fffffff;

    private readonly uint[] _mt = new uint[N];
    private int _indice = N;

    public AleatorioPython(int semilla)
    {
        Iniciar(19650218);
        var clave = new[] { (uint)semilla };
        var i = 1;
        var j = 0;
        for (var k = Math.Max(N, clave.Length); k > 0; k--)
        {
            _mt[i] = (_mt[i] ^ ((_mt[i - 1] ^ (_mt[i - 1] >> 30)) * 1664525u)) + clave[j] + (uint)j;
            i++;
            j++;
            if (i >= N)
            {
                _mt[0] = _mt[N - 1];
                i = 1;
            }

            if (j >= clave.Length)
            {
                j = 0;
            }
        }

        for (var k = N - 1; k > 0; k--)
        {
            _mt[i] = (_mt[i] ^ ((_mt[i - 1] ^ (_mt[i - 1] >> 30)) * 1566083941u)) - (uint)i;
            i++;
            if (i >= N)
            {
                _mt[0] = _mt[N - 1];
                i = 1;
            }
        }

        _mt[0] = Upper;
        _indice = N;
    }

    public double Siguiente()
    {
        var a = Entero() >> 5;
        var b = Entero() >> 6;
        return (a * 67108864.0 + b) * (1.0 / 9007199254740992.0);
    }

    public int EnteroIncluyente(int minimo, int maximo) => minimo + PorDebajo(maximo - minimo + 1);

    public double Uniforme(double minimo, double maximo) => minimo + (maximo - minimo) * Siguiente();

    public T Elegir<T>(IReadOnlyList<T> poblacion) => poblacion[PorDebajo(poblacion.Count)];

    public T ElegirPonderado<T>(IReadOnlyList<T> poblacion, IReadOnlyList<double> pesos)
    {
        var acum = new double[pesos.Count];
        double total = 0;
        for (var i = 0; i < pesos.Count; i++)
        {
            total += pesos[i];
            acum[i] = total;
        }

        var marca = Siguiente() * total;
        var lo = 0;
        var hi = poblacion.Count - 1;
        while (lo < hi)
        {
            var medio = (lo + hi) / 2;
            if (marca < acum[medio])
            {
                hi = medio;
            }
            else
            {
                lo = medio + 1;
            }
        }

        return poblacion[lo];
    }

    public List<T> Muestra<T>(IReadOnlyList<T> poblacion, int cantidad)
    {
        var n = poblacion.Count;
        var resultado = new List<T>(cantidad);
        var tamano = 21;
        if (cantidad > 5)
        {
            tamano += (int)Math.Pow(4, Math.Ceiling(Math.Log(cantidad * 3, 4)));
        }

        if (n <= tamano)
        {
            var bolsa = poblacion.ToList();
            for (var i = 0; i < cantidad; i++)
            {
                var j = PorDebajo(n - i);
                resultado.Add(bolsa[j]);
                bolsa[j] = bolsa[n - i - 1];
            }

            return resultado;
        }

        var elegidos = new HashSet<int>();
        for (var i = 0; i < cantidad; i++)
        {
            var j = PorDebajo(n);
            while (!elegidos.Add(j))
            {
                j = PorDebajo(n);
            }

            resultado.Add(poblacion[j]);
        }

        return resultado;
    }

    private int PorDebajo(int n)
    {
        var bits = BitLength(n);
        var valor = Bits(bits);
        while (valor >= (uint)n)
        {
            valor = Bits(bits);
        }

        return (int)valor;
    }

    private uint Bits(int cantidad)
    {
        if (cantidad <= 0)
        {
            return 0;
        }

        return Entero() >> (32 - cantidad);
    }

    private uint Entero()
    {
        if (_indice >= N)
        {
            Mezclar();
        }

        var y = _mt[_indice++];
        y ^= y >> 11;
        y ^= (y << 7) & 0x9d2c5680u;
        y ^= (y << 15) & 0xefc60000u;
        y ^= y >> 18;
        return y;
    }

    private void Mezclar()
    {
        for (var k = 0; k < N - M; k++)
        {
            var y = (_mt[k] & Upper) | (_mt[k + 1] & Lower);
            _mt[k] = _mt[k + M] ^ (y >> 1) ^ ((y & 1) == 0 ? 0 : MatrixA);
        }

        for (var k = N - M; k < N - 1; k++)
        {
            var y = (_mt[k] & Upper) | (_mt[k + 1] & Lower);
            _mt[k] = _mt[k + (M - N)] ^ (y >> 1) ^ ((y & 1) == 0 ? 0 : MatrixA);
        }

        var ultimo = (_mt[N - 1] & Upper) | (_mt[0] & Lower);
        _mt[N - 1] = _mt[M - 1] ^ (ultimo >> 1) ^ ((ultimo & 1) == 0 ? 0 : MatrixA);
        _indice = 0;
    }

    private void Iniciar(uint semilla)
    {
        _mt[0] = semilla;
        for (var i = 1; i < N; i++)
        {
            _mt[i] = 1812433253u * (_mt[i - 1] ^ (_mt[i - 1] >> 30)) + (uint)i;
        }

        _indice = N;
    }

    private static int BitLength(int n)
    {
        var bits = 0;
        var valor = (uint)n;
        while (valor > 0)
        {
            bits++;
            valor >>= 1;
        }

        return bits;
    }
}
