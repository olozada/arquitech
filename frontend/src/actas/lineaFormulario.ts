import Swal from 'sweetalert2'
import { abrirPdf } from '../api/archivos'
import { mensajeError } from '../api/cliente'
import { numero, redondear } from '../actas/formato'
import type { Acta } from '../api/actas'
import type { LineaFormulario } from '../actas/lineaActa'
import type { LineaBorrador } from '../actas/useBorradorActa'
import { perfilArquitecto } from '../auth/perfiles'
import type { RegistroCatalogo } from '../catalogos/definiciones'

const mensajeFechas = 'La fecha final debe ser mayor o igual que la fecha inicial.'

export function imprimir(numeroActa: number) {
  const ventana = prepararVisor()
  void abrirPdf(numeroActa, ventana).catch(async (error) => {
    ventana?.close()
    await avisar(mensajeError(error))
  })
}

function prepararVisor() {
  const ventana = window.open('', '_blank')
  if (!ventana) {
    return null
  }

  ventana.document.write(
    '<!DOCTYPE html><title>Acta</title><p style="font-family:sans-serif;padding:24px">Generando el acta…</p>',
  )
  ventana.document.close()
  return ventana
}

export type ActaGuardada = Acta

export function construirLinea(
  formulario: LineaFormulario,
  construcciones: RegistroCatalogo[] | undefined,
  conceptos: RegistroCatalogo[] | undefined,
  unidades: RegistroCatalogo[] | undefined,
  insumos: RegistroCatalogo[] | undefined,
): { ok: true; linea: Omit<LineaBorrador, 'clave'> } | { ok: false; error: string } {
  const construccion = buscar(construcciones, Number(formulario.construccionId))
  const concepto = buscar(conceptos, Number(formulario.conceptoId))
  const unidad = buscar(unidades, Number(formulario.unidadMedidaId))
  const insumo = formulario.insumoId ? buscar(insumos, Number(formulario.insumoId)) : null
  const cantidad = numero(formulario.cantidad)
  const precio = numero(formulario.precio)
  const descripcion = formulario.descripcion.trim()
  const faltantes: string[] = []
  if (!construccion) {
    faltantes.push('la construcción')
  }
  if (!concepto) {
    faltantes.push('el concepto')
  }
  if (formulario.unidadMedidaId && !unidad) {
    faltantes.push('la unidad de medida')
  }
  if (formulario.insumoId && !insumo) {
    faltantes.push('el insumo')
  }
  if (cantidad == null || cantidad <= 0) {
    faltantes.push('la cantidad')
  }
  if (precio == null || precio < 0) {
    faltantes.push('el precio')
  }
  if (faltantes.length > 0 || !construccion || !concepto || cantidad == null || precio == null) {
    return { ok: false, error: `Debe indicar ${enumerar(faltantes)}.` }
  }

  return {
    ok: true,
    linea: {
        id: null,
        tipoUsuarioId: perfilArquitecto,
        tipoUsuario: 'Arquitecto',
        construccionId: construccion.id,
      construccion: String(construccion.descripcion ?? ''),
      conceptoId: concepto.id,
      concepto: String(concepto.descripcion ?? ''),
      unidadMedidaId: unidad?.id ?? null,
      unidadMedida: unidad ? String(unidad.nombre ?? '') : '',
      insumoId: insumo?.id ?? null,
      insumo: insumo ? String(insumo.descripcion ?? '') : null,
      cantidad: redondear(cantidad, 4),
      precio: redondear(precio, 2),
      descripcion,
      aprobado: true,
      presupuestoItemId: Number(formulario.presupuestoItemId) > 0 ? Number(formulario.presupuestoItemId) : null,
    },
  }
}

function buscar(opciones: RegistroCatalogo[] | undefined, id: number) {
  return (opciones ?? []).find((opcion) => opcion.id === id)
}

export function validarEncabezado(
  fechaInicial: string,
  fechaFinal: string,
  arquitectoId: number,
  revisorId: number,
  contratistaId: number,
) {
  const faltantes: string[] = []
  if (!fechaInicial) {
    faltantes.push('la fecha inicial')
  }
  if (!fechaFinal) {
    faltantes.push('la fecha final')
  }
  if (arquitectoId <= 0) {
    faltantes.push('el arquitecto')
  }
  if (revisorId <= 0) {
    faltantes.push('el revisor')
  }
  if (contratistaId <= 0) {
    faltantes.push('el contratista')
  }
  if (faltantes.length > 0) {
    return `Debe seleccionar ${enumerar(faltantes)}.`
  }

  if (fechaFinal < fechaInicial) {
    return mensajeFechas
  }

  return null
}

function enumerar(partes: string[]) {
  if (partes.length === 1) {
    return partes[0]
  }

  return `${partes.slice(0, -1).join(', ')} y ${partes[partes.length - 1]}`
}

export function lineaDesdeServidor(item: ActaGuardada['detalles'][number]): LineaBorrador {
  return {
    clave: `srv-${item.id}`,
    id: item.id,
    tipoUsuarioId: item.tipoUsuarioId,
    tipoUsuario: item.tipoUsuario,
    construccionId: item.construccionId,
    construccion: item.construccion,
    conceptoId: item.conceptoId,
    concepto: item.concepto,
    unidadMedidaId: item.unidadMedidaId ?? null,
    unidadMedida: item.unidadMedida,
    insumoId: item.insumoId ?? null,
    insumo: item.insumo ?? null,
    cantidad: item.cantidad,
    precio: item.precio,
    descripcion: item.descripcion,
    aprobado: item.aprobado !== false,
    presupuestoItemId: item.presupuestoItemId ?? null,
  }
}

export function avisar(texto: string) {
  return Swal.fire({ icon: 'error', title: '¡Error!', text: texto, confirmButtonColor: '#1abb9c' })
}
