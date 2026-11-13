import 'dart:convert';

class ResumenCostoPresupuesto {
  const ResumenCostoPresupuesto({
    required this.subtotal,
    required this.totalMaterial,
    required this.totalManoObra,
    required this.imprevistos,
    required this.incremento,
    required this.administracion,
    required this.utilidad,
    required this.ivaUtilidad,
    required this.total,
    this.valorPorM2,
  });

  final double subtotal;
  final double totalMaterial;
  final double totalManoObra;
  final double imprevistos;
  final double incremento;
  final double administracion;
  final double utilidad;
  final double ivaUtilidad;
  final double total;
  final double? valorPorM2;

  factory ResumenCostoPresupuesto.desdeJson(Map<String, Object?> mapa) {
    return ResumenCostoPresupuesto(
      subtotal: _numero(mapa['subtotal']),
      totalMaterial: _numero(mapa['totalMaterial']),
      totalManoObra: _numero(mapa['totalManoObra']),
      imprevistos: _numero(mapa['imprevistos']),
      incremento: _numero(mapa['incremento']),
      administracion: _numero(mapa['administracion']),
      utilidad: _numero(mapa['utilidad']),
      ivaUtilidad: _numero(mapa['ivaUtilidad']),
      total: _numero(mapa['total']),
      valorPorM2: _numeroOpcional(mapa['valorPorM2']),
    );
  }
}

class ItemPresupuestoVista {
  const ItemPresupuestoVista({
    required this.id,
    required this.codigo,
    required this.descripcion,
    required this.unidad,
    required this.cantidad,
    required this.valorUnitario,
    required this.total,
  });

  final int id;
  final String codigo;
  final String descripcion;
  final String unidad;
  final double cantidad;
  final double valorUnitario;
  final double total;

  factory ItemPresupuestoVista.desdeJson(Map<String, Object?> mapa) {
    return ItemPresupuestoVista(
      id: _entero(mapa['id']),
      codigo: mapa['codigo']?.toString() ?? '',
      descripcion: mapa['descripcion']?.toString() ?? '',
      unidad: mapa['unidad']?.toString() ?? '',
      cantidad: _numero(mapa['cantidad']),
      valorUnitario: _numero(mapa['valorUnitario']),
      total: _numero(mapa['total']),
    );
  }
}

class CapituloPresupuestoVista {
  const CapituloPresupuestoVista({
    required this.id,
    required this.numero,
    required this.nombre,
    required this.total,
    required this.items,
  });

  final int id;
  final int numero;
  final String nombre;
  final double total;
  final List<ItemPresupuestoVista> items;

  factory CapituloPresupuestoVista.desdeJson(Map<String, Object?> mapa) {
    final itemsJson = mapa['items'] as List? ?? const [];
    return CapituloPresupuestoVista(
      id: _entero(mapa['id']),
      numero: _entero(mapa['numero']),
      nombre: mapa['nombre']?.toString() ?? '',
      total: _numero(mapa['total']),
      items: itemsJson
          .map(
            (item) => ItemPresupuestoVista.desdeJson(
              Map<String, Object?>.from(item as Map),
            ),
          )
          .toList(),
    );
  }
}

class PresupuestoDetalleVista {
  const PresupuestoDetalleVista({
    required this.id,
    required this.construccion,
    required this.nombre,
    required this.version,
    required this.estado,
    required this.fecha,
    required this.resumen,
    required this.capitulos,
  });

  final int id;
  final String construccion;
  final String nombre;
  final int version;
  final String estado;
  final String fecha;
  final ResumenCostoPresupuesto resumen;
  final List<CapituloPresupuestoVista> capitulos;

  factory PresupuestoDetalleVista.desdeJson(String json) {
    final mapa = Map<String, Object?>.from(jsonDecode(json) as Map);
    final capitulosJson = mapa['capitulos'] as List? ?? const [];
    return PresupuestoDetalleVista(
      id: _entero(mapa['id']),
      construccion: mapa['construccion']?.toString() ?? '',
      nombre: mapa['nombre']?.toString() ?? '',
      version: _entero(mapa['version']),
      estado: mapa['estado']?.toString() ?? '',
      fecha: mapa['fecha']?.toString() ?? '',
      resumen: ResumenCostoPresupuesto.desdeJson(
        Map<String, Object?>.from(mapa['resumen'] as Map? ?? const {}),
      ),
      capitulos: capitulosJson
          .map(
            (cap) => CapituloPresupuestoVista.desdeJson(
              Map<String, Object?>.from(cap as Map),
            ),
          )
          .toList(),
    );
  }
}

class AvisoValidacionPresupuesto {
  const AvisoValidacionPresupuesto({
    this.itemId,
    required this.codigo,
    required this.tipo,
    required this.mensaje,
  });

  final int? itemId;
  final String codigo;
  final String tipo;
  final String mensaje;

  factory AvisoValidacionPresupuesto.desdeJson(Map<String, Object?> mapa) {
    return AvisoValidacionPresupuesto(
      itemId: _enteroOpcional(mapa['itemId']),
      codigo: mapa['codigo']?.toString() ?? '',
      tipo: mapa['tipo']?.toString() ?? '',
      mensaje: mapa['mensaje']?.toString() ?? '',
    );
  }
}

List<AvisoValidacionPresupuesto> parsearValidacion(String? json) {
  if (json == null || json.trim().isEmpty) {
    return const [];
  }
  final lista = jsonDecode(json) as List;
  return lista
      .map(
        (item) => AvisoValidacionPresupuesto.desdeJson(
          Map<String, Object?>.from(item as Map),
        ),
      )
      .toList();
}

class PuntoEjecucionMensual {
  const PuntoEjecucionMensual({
    required this.anio,
    required this.mes,
    required this.ejecutado,
    required this.acumulado,
  });

  final int anio;
  final int mes;
  final double ejecutado;
  final double acumulado;

  String get etiqueta => '$mes/$anio';

  factory PuntoEjecucionMensual.desdeJson(Map<String, Object?> mapa) {
    return PuntoEjecucionMensual(
      anio: _entero(mapa['anio']),
      mes: _entero(mapa['mes']),
      ejecutado: _numero(mapa['ejecutado']),
      acumulado: _numero(mapa['acumulado']),
    );
  }
}

class FilaSeguimientoPresupuesto {
  const FilaSeguimientoPresupuesto({
    this.itemId,
    this.capituloId,
    required this.capitulo,
    required this.codigo,
    required this.descripcion,
    required this.unidad,
    required this.cantidadPresupuestada,
    required this.cantidadEjecutada,
    required this.avanceFisico,
    required this.valorPresupuestado,
    required this.valorEjecutado,
    required this.saldo,
    required this.precioActas,
    required this.proyeccionCierre,
  });

  final int? itemId;
  final int? capituloId;
  final String capitulo;
  final String codigo;
  final String descripcion;
  final String unidad;
  final double cantidadPresupuestada;
  final double cantidadEjecutada;
  final double avanceFisico;
  final double valorPresupuestado;
  final double valorEjecutado;
  final double saldo;
  final double precioActas;
  final double proyeccionCierre;

  factory FilaSeguimientoPresupuesto.desdeJson(Map<String, Object?> mapa) {
    return FilaSeguimientoPresupuesto(
      itemId: _enteroOpcional(mapa['itemId']),
      capituloId: _enteroOpcional(mapa['capituloId']),
      capitulo: mapa['capitulo']?.toString() ?? '',
      codigo: mapa['codigo']?.toString() ?? '',
      descripcion: mapa['descripcion']?.toString() ?? '',
      unidad: mapa['unidad']?.toString() ?? '',
      cantidadPresupuestada: _numero(mapa['cantidadPresupuestada']),
      cantidadEjecutada: _numero(mapa['cantidadEjecutada']),
      avanceFisico: _numero(mapa['avanceFisico']),
      valorPresupuestado: _numero(mapa['valorPresupuestado']),
      valorEjecutado: _numero(mapa['valorEjecutado']),
      saldo: _numero(mapa['saldo']),
      precioActas: _numero(mapa['precioActas']),
      proyeccionCierre: _numero(mapa['proyeccionCierre']),
    );
  }
}

class SeguimientoPresupuestoVista {
  const SeguimientoPresupuestoVista({
    required this.construccion,
    required this.resumen,
    required this.ejecutado,
    required this.proyeccion,
    required this.noPresupuestado,
    required this.items,
    required this.noPresupuestados,
    required this.acumuladoMensual,
  });

  final String construccion;
  final ResumenCostoPresupuesto resumen;
  final double ejecutado;
  final double proyeccion;
  final double noPresupuestado;
  final List<FilaSeguimientoPresupuesto> items;
  final List<FilaSeguimientoPresupuesto> noPresupuestados;
  final List<PuntoEjecucionMensual> acumuladoMensual;

  factory SeguimientoPresupuestoVista.desdeJson(String json) {
    final mapa = Map<String, Object?>.from(jsonDecode(json) as Map);
    List<FilaSeguimientoPresupuesto> filasDe(Object? lista) {
      if (lista is! List) {
        return const [];
      }
      return lista
          .map(
            (fila) => FilaSeguimientoPresupuesto.desdeJson(
              Map<String, Object?>.from(fila as Map),
            ),
          )
          .toList();
    }

    final mensualJson = mapa['acumuladoMensual'] as List? ?? const [];
    final mensual = mensualJson
        .map(
          (punto) => PuntoEjecucionMensual.desdeJson(
            Map<String, Object?>.from(punto as Map),
          ),
        )
        .toList();

    return SeguimientoPresupuestoVista(
      construccion: mapa['construccion']?.toString() ?? '',
      resumen: ResumenCostoPresupuesto.desdeJson(
        Map<String, Object?>.from(mapa['resumen'] as Map? ?? const {}),
      ),
      ejecutado: _numero(mapa['ejecutado']),
      proyeccion: _numero(mapa['proyeccion']),
      noPresupuestado: _numero(mapa['noPresupuestado']),
      items: filasDe(mapa['items']),
      noPresupuestados: filasDe(mapa['noPresupuestados']),
      acumuladoMensual: mensual,
    );
  }
}

double _numero(Object? valor) {
  if (valor == null) {
    return 0;
  }
  if (valor is num) {
    return valor.toDouble();
  }
  return double.tryParse(valor.toString()) ?? 0;
}

double? _numeroOpcional(Object? valor) {
  if (valor == null) {
    return null;
  }
  if (valor is num) {
    return valor.toDouble();
  }
  return double.tryParse(valor.toString());
}

int _entero(Object? valor) {
  if (valor is int) {
    return valor;
  }
  if (valor is num) {
    return valor.toInt();
  }
  return int.parse(valor.toString());
}

int? _enteroOpcional(Object? valor) {
  if (valor == null) {
    return null;
  }
  return _entero(valor);
}
