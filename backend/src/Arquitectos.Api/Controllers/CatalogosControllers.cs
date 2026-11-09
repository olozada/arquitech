using Arquitectos.Application.Catalogos;
using Arquitectos.Domain.Entidades;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Arquitectos.Api.Controllers;

[Route("api/arquitectos")]
public sealed class ArquitectosController : CatalogoController<Arquitecto, CatalogoNombreDto, CatalogoNombreSolicitud>
{
    public ArquitectosController(
        ICatalogoServicio<Arquitecto, CatalogoNombreDto, CatalogoNombreSolicitud> servicio,
        IValidator<CatalogoNombreSolicitud> validador)
        : base(servicio, validador)
    {
    }
}

[Route("api/revisores")]
public sealed class RevisoresController : CatalogoController<Revisor, CatalogoNombreDto, CatalogoNombreSolicitud>
{
    public RevisoresController(
        ICatalogoServicio<Revisor, CatalogoNombreDto, CatalogoNombreSolicitud> servicio,
        IValidator<CatalogoNombreSolicitud> validador)
        : base(servicio, validador)
    {
    }
}

[Route("api/contratistas")]
public sealed class ContratistasController : CatalogoController<Contratista, CatalogoNombreDto, CatalogoNombreSolicitud>
{
    public ContratistasController(
        ICatalogoServicio<Contratista, CatalogoNombreDto, CatalogoNombreSolicitud> servicio,
        IValidator<CatalogoNombreSolicitud> validador)
        : base(servicio, validador)
    {
    }
}

[Route("api/proveedores")]
public sealed class ProveedoresController : CatalogoController<Proveedor, CatalogoNombreDto, CatalogoNombreSolicitud>
{
    public ProveedoresController(
        ICatalogoServicio<Proveedor, CatalogoNombreDto, CatalogoNombreSolicitud> servicio,
        IValidator<CatalogoNombreSolicitud> validador)
        : base(servicio, validador)
    {
    }
}

[Route("api/construcciones")]
public sealed class ConstruccionesController : CatalogoController<Construccion, ConstruccionDto, ConstruccionSolicitud>
{
    public ConstruccionesController(
        ICatalogoServicio<Construccion, ConstruccionDto, ConstruccionSolicitud> servicio,
        IValidator<ConstruccionSolicitud> validador)
        : base(servicio, validador)
    {
    }
}

[Route("api/conceptos")]
public sealed class ConceptosController : CatalogoController<Concepto, DescripcionUnidadDto, DescripcionUnidadSolicitud>
{
    public ConceptosController(
        ICatalogoServicio<Concepto, DescripcionUnidadDto, DescripcionUnidadSolicitud> servicio,
        IValidator<DescripcionUnidadSolicitud> validador)
        : base(servicio, validador)
    {
    }
}

[Route("api/materiales")]
public sealed class MaterialesController : CatalogoController<Material, DescripcionUnidadDto, DescripcionUnidadSolicitud>
{
    public MaterialesController(
        ICatalogoServicio<Material, DescripcionUnidadDto, DescripcionUnidadSolicitud> servicio,
        IValidator<DescripcionUnidadSolicitud> validador)
        : base(servicio, validador)
    {
    }
}

[Route("api/unidades-medida")]
public sealed class UnidadesMedidaController : CatalogoController<UnidadMedida, UnidadMedidaDto, UnidadMedidaSolicitud>
{
    public UnidadesMedidaController(
        ICatalogoServicio<UnidadMedida, UnidadMedidaDto, UnidadMedidaSolicitud> servicio,
        IValidator<UnidadMedidaSolicitud> validador)
        : base(servicio, validador)
    {
    }
}
