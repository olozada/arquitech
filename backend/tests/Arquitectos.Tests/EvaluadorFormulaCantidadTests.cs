using Arquitectos.Application.Presupuestos;

namespace Arquitectos.Tests;

public class EvaluadorFormulaCantidadTests
{
  private static ItemCantidadFormula F(int id, string codigo, string? formula, decimal manual = 0) =>
      new(id, codigo, formula, manual);

    [Fact]
    public void Aritmetica_y_coma_decimal()
    {
        var r = EvaluadorFormulaCantidad.Evaluar([F(1, "1.01", "=12*3,5")]);
        Assert.Equal(42, r.Cantidades[1]);
        Assert.Empty(r.Avisos);
    }

    [Fact]
    public void Referencia_simple()
    {
        var r = EvaluadorFormulaCantidad.Evaluar([
            F(1, "1.01", null, 100),
            F(2, "1.02", "=[1.01]*1,35"),
        ]);
        Assert.Equal(135, r.Cantidades[2]);
    }

    [Fact]
    public void Cadena_de_tres_niveles()
    {
        var r = EvaluadorFormulaCantidad.Evaluar([
            F(1, "a", null, 2),
            F(2, "b", "=[a]*3"),
            F(3, "c", "=[b]+1"),
        ]);
        Assert.Equal(7, r.Cantidades[3]);
    }

    [Fact]
    public void Ciclo_detectado()
    {
        var r = EvaluadorFormulaCantidad.Evaluar([
            F(1, "1.01", "=[1.02]"),
            F(2, "1.02", "=[1.01]"),
        ]);
        Assert.Contains(r.Avisos, aviso => aviso.Tipo == "FormulaCiclica");
    }

    [Fact]
    public void Referencia_inexistente()
    {
        var r = EvaluadorFormulaCantidad.Evaluar([F(1, "1.01", "=[9.99]*2")]);
        Assert.Contains(r.Avisos, aviso => aviso.Tipo == "FormulaReferenciaInvalida");
    }
}