import 'package:decimal/decimal.dart';

import 'base_local.dart';

enum EstadoActaFiltro { todas, abiertas, cerradas }

enum OrdenActas { fechaInicial, fechaFinal, numero }

class FiltroActas {
  const FiltroActas({
    this.texto = '',
    this.estado = EstadoActaFiltro.todas,
    this.desde,
    this.hasta,
    this.obraId,
    this.orden = OrdenActas.fechaInicial,
  });

  final String texto;
  final EstadoActaFiltro estado;
  final String? desde;
  final String? hasta;
  final int? obraId;
  final OrdenActas orden;
}

class ObraOpcion {
  const ObraOpcion({required this.id, required this.descripcion});

  final int id;
  final String descripcion;
}

class ActaResumen {
  const ActaResumen({
    required this.numero,
    required this.fechaInicial,
    required this.fechaFinal,
    required this.arquitecto,
    required this.revisor,
    required this.contratista,
    required this.cerrada,
    required this.totalAprobado,
    required this.cambiosSinSincronizar,
  });

  final int numero;
  final String fechaInicial;
  final String fechaFinal;
  final String arquitecto;
  final String revisor;
  final String contratista;
  final bool cerrada;
  final Decimal totalAprobado;
  final bool cambiosSinSincronizar;
}

class LineaDetalle {
  const LineaDetalle({
    required this.localId,
    required this.construccionId,
    required this.construccion,
    required this.conceptoId,
    required this.tipoUsuarioId,
    required this.perfil,
    required this.descripcion,
    required this.cantidad,
    required this.precio,
    required this.subtotal,
    required this.aprobado,
    required this.estadoLocal,
    this.unidadMedidaId,
    this.insumoId,
    this.presupuestoItemId,
  });

  final String localId;
  final int construccionId;
  final String construccion;
  final int conceptoId;
  final int tipoUsuarioId;
  final String perfil;
  final String descripcion;
  final String cantidad;
  final String precio;
  final String subtotal;
  final bool aprobado;
  final String estadoLocal;
  final int? unidadMedidaId;
  final int? insumoId;
  final int? presupuestoItemId;
}

class OpcionCatalogo {
  const OpcionCatalogo({required this.id, required this.descripcion});

  final int id;
  final String descripcion;
}

class InsumoOpcion {
  const InsumoOpcion({
    required this.id,
    required this.descripcion,
    this.codigo,
  });

  final int id;
  final String descripcion;
  final String? codigo;

  String get etiqueta {
    final cod = codigo?.trim();
    if (cod == null || cod.isEmpty) {
      return descripcion;
    }
    return '$cod $descripcion';
  }
}

class ResumenCola {
  const ResumenCola({
    required this.idOperacion,
    required this.tipo,
    required this.numeroActa,
    this.localId,
    required this.secuencia,
  });

  final String idOperacion;
  final String tipo;
  final int numeroActa;
  final String? localId;
  final int secuencia;
}

class ItemPresupuestoOpcion {
  const ItemPresupuestoOpcion({
    required this.id,
    required this.descripcion,
    required this.conceptoId,
    this.unidadMedidaId,
    this.precio,
  });

  final int id;
  final String descripcion;
  final int conceptoId;
  final int? unidadMedidaId;
  final String? precio;
}

class DetalleActa {
  const DetalleActa({
    required this.numero,
    required this.fechaInicial,
    required this.fechaFinal,
    required this.arquitecto,
    required this.revisor,
    required this.contratista,
    required this.observacion,
    required this.cerrada,
    required this.totalAprobado,
    required this.lineas,
  });

  final int numero;
  final String fechaInicial;
  final String fechaFinal;
  final String arquitecto;
  final String revisor;
  final String contratista;
  final String? observacion;
  final bool cerrada;
  final Decimal totalAprobado;
  final List<LineaDetalle> lineas;
}

class PresupuestoResumen {
  const PresupuestoResumen({
    required this.id,
    required this.nombre,
    required this.construccion,
    required this.estado,
    required this.total,
  });

  final int id;
  final String nombre;
  final String construccion;
  final String estado;
  final String? total;
}

class PresupuestoPaquete {
  const PresupuestoPaquete({
    required this.id,
    required this.nombre,
    required this.construccion,
    required this.estado,
    required this.detalleJson,
    this.validacionJson,
    this.seguimientoJson,
  });

  final int id;
  final String nombre;
  final String construccion;
  final String estado;
  final String detalleJson;
  final String? validacionJson;
  final String? seguimientoJson;
}

class ConsultasLocales {
  ConsultasLocales(this.base);

  final BaseLocal base;

  Future<int> pendientes() async {
    final filas = await base.consultar(
      "SELECT COUNT(*) AS total FROM outbox WHERE estado IN ('pendiente', 'enviando')",
    );
    return _entero(filas.first['total']);
  }

  Future<List<ObraOpcion>> obras() async {
    final filas = await base.consultar(
      'SELECT id, descripcion FROM construcciones ORDER BY descripcion',
    );
    return filas
        .map(
          (fila) => ObraOpcion(
            id: _entero(fila['id']),
            descripcion: _texto(fila['descripcion']),
          ),
        )
        .toList();
  }

  Future<List<ActaResumen>> listarActas(FiltroActas filtro) async {
    final actas = await base.consultar('''
SELECT a.numero_acta, a.fecha_inicial, a.fecha_final, a.cerrada,
       ar.nombre AS arquitecto, re.nombre AS revisor, co.nombre AS contratista
FROM actas a
JOIN arquitectos ar ON ar.id = a.arquitecto_id
JOIN revisores re ON re.id = a.revisor_id
JOIN contratistas co ON co.id = a.contratista_id
''');
    final lineas = await base.consultar(
      'SELECT numero_acta, construccion_id, subtotal, aprobado, estado_local FROM acta_detalles',
    );
    final cola = await base.consultar(
      "SELECT numero_acta FROM outbox WHERE estado IN ('pendiente', 'enviando')",
    );
    final enCola = cola.map((fila) => _entero(fila['numero_acta'])).toSet();
    final porActa = <int, List<Map<String, Object?>>>{};
    for (final linea in lineas) {
      porActa.putIfAbsent(_entero(linea['numero_acta']), () => []).add(linea);
    }

    final busqueda = filtro.texto.trim().toLowerCase();
    final resultado = <ActaResumen>[];
    for (final fila in actas) {
      final numero = _entero(fila['numero_acta']);
      final fechaInicial = _texto(fila['fecha_inicial']);
      final fechaFinal = _texto(fila['fecha_final']);
      final cerrada = _entero(fila['cerrada']) == 1;
      final arquitecto = _texto(fila['arquitecto']);
      final revisor = _texto(fila['revisor']);
      final contratista = _texto(fila['contratista']);
      if (filtro.estado == EstadoActaFiltro.abiertas && cerrada) {
        continue;
      }
      if (filtro.estado == EstadoActaFiltro.cerradas && !cerrada) {
        continue;
      }
      if (filtro.desde != null && fechaFinal.compareTo(filtro.desde!) < 0) {
        continue;
      }
      if (filtro.hasta != null && fechaInicial.compareTo(filtro.hasta!) > 0) {
        continue;
      }
      if (busqueda.isNotEmpty) {
        final texto = '$numero $arquitecto $revisor $contratista'.toLowerCase();
        if (!texto.contains(busqueda)) {
          continue;
        }
      }

      final propias = porActa[numero] ?? const <Map<String, Object?>>[];
      if (filtro.obraId != null &&
          !propias.any(
            (linea) =>
                _texto(linea['estado_local']) != 'eliminado' &&
                _entero(linea['construccion_id']) == filtro.obraId,
          )) {
        continue;
      }

      var total = Decimal.zero;
      var cambios = enCola.contains(numero);
      for (final linea in propias) {
        final estado = _texto(linea['estado_local']);
        if (estado != 'sincronizado') {
          cambios = true;
        }
        if (estado == 'eliminado' || _entero(linea['aprobado']) != 1) {
          continue;
        }
        total += Decimal.parse(_texto(linea['subtotal']));
      }

      resultado.add(
        ActaResumen(
          numero: numero,
          fechaInicial: fechaInicial,
          fechaFinal: fechaFinal,
          arquitecto: arquitecto,
          revisor: revisor,
          contratista: contratista,
          cerrada: cerrada,
          totalAprobado: total,
          cambiosSinSincronizar: cambios,
        ),
      );
    }

    resultado.sort((a, b) {
      final comparacion = switch (filtro.orden) {
        OrdenActas.fechaInicial => a.fechaInicial.compareTo(b.fechaInicial),
        OrdenActas.fechaFinal => a.fechaFinal.compareTo(b.fechaFinal),
        OrdenActas.numero => a.numero.compareTo(b.numero),
      };
      return -comparacion;
    });
    return resultado;
  }

  Future<DetalleActa?> detalleActa(int numero) async {
    final actas = await base.consultar(
      '''
SELECT a.numero_acta, a.fecha_inicial, a.fecha_final, a.observacion, a.cerrada,
       ar.nombre AS arquitecto, re.nombre AS revisor, co.nombre AS contratista
FROM actas a
JOIN arquitectos ar ON ar.id = a.arquitecto_id
JOIN revisores re ON re.id = a.revisor_id
JOIN contratistas co ON co.id = a.contratista_id
WHERE a.numero_acta = ?
''',
      [numero],
    );
    if (actas.isEmpty) {
      return null;
    }
    final acta = actas.first;
    final lineas = await base.consultar(
      '''
SELECT d.local_id, d.construccion_id, d.concepto_id, d.unidad_medida_id, d.insumo_id,
       d.presupuesto_item_id, d.tipo_usuario_id, d.cantidad, d.precio, d.subtotal,
       d.descripcion, d.aprobado, d.estado_local,
       c.descripcion AS construccion, t.nombre AS perfil
FROM acta_detalles d
JOIN construcciones c ON c.id = d.construccion_id
JOIN tipos_usuario t ON t.id = d.tipo_usuario_id
WHERE d.numero_acta = ? AND d.estado_local != 'eliminado'
ORDER BY c.descripcion, d.local_id
''',
      [numero],
    );
    var total = Decimal.zero;
    final visibles = <LineaDetalle>[];
    for (final fila in lineas) {
      final subtotal = Decimal.parse(_texto(fila['subtotal']));
      if (_entero(fila['aprobado']) == 1) {
        total += subtotal;
      }
      visibles.add(
        LineaDetalle(
          localId: _texto(fila['local_id']),
          construccionId: _entero(fila['construccion_id']),
          construccion: _texto(fila['construccion']),
          conceptoId: _entero(fila['concepto_id']),
          tipoUsuarioId: _entero(fila['tipo_usuario_id']),
          perfil: _texto(fila['perfil']),
          descripcion: _texto(fila['descripcion']),
          cantidad: _texto(fila['cantidad']),
          precio: _texto(fila['precio']),
          subtotal: _texto(fila['subtotal']),
          aprobado: _entero(fila['aprobado']) == 1,
          estadoLocal: _texto(fila['estado_local']),
          unidadMedidaId: _opcional(fila['unidad_medida_id']),
          insumoId: _opcional(fila['insumo_id']),
          presupuestoItemId: _opcional(fila['presupuesto_item_id']),
        ),
      );
    }

    final observacion = acta['observacion']?.toString();
    return DetalleActa(
      numero: _entero(acta['numero_acta']),
      fechaInicial: _texto(acta['fecha_inicial']),
      fechaFinal: _texto(acta['fecha_final']),
      arquitecto: _texto(acta['arquitecto']),
      revisor: _texto(acta['revisor']),
      contratista: _texto(acta['contratista']),
      observacion: observacion == null || observacion.isEmpty
          ? null
          : observacion,
      cerrada: _entero(acta['cerrada']) == 1,
      totalAprobado: total,
      lineas: visibles,
    );
  }

  Future<List<PresupuestoResumen>> presupuestos() async {
    final filas = await base.consultar('''
SELECT id, nombre, construccion, estado, total
FROM presupuestos
ORDER BY nombre
''');
    return filas
        .map(
          (fila) => PresupuestoResumen(
            id: _entero(fila['id']),
            nombre: _texto(fila['nombre']),
            construccion: _texto(fila['construccion']),
            estado: _texto(fila['estado']),
            total: fila['total']?.toString(),
          ),
        )
        .toList();
  }

  Future<PresupuestoPaquete?> presupuestoPorId(int id) async {
    final filas = await base.consultar(
      '''
SELECT id, nombre, construccion, estado, detalle_json, validacion_json, seguimiento_json
FROM presupuestos
WHERE id = ?
''',
      [id],
    );
    if (filas.isEmpty) {
      return null;
    }
    final fila = filas.first;
    return PresupuestoPaquete(
      id: _entero(fila['id']),
      nombre: _texto(fila['nombre']),
      construccion: _texto(fila['construccion']),
      estado: _texto(fila['estado']),
      detalleJson: _texto(fila['detalle_json']),
      validacionJson: fila['validacion_json']?.toString(),
      seguimientoJson: fila['seguimiento_json']?.toString(),
    );
  }

  Future<List<OpcionCatalogo>> construcciones() async {
    final filas = await base.consultar(
      'SELECT id, descripcion FROM construcciones ORDER BY descripcion',
    );
    return filas
        .map(
          (fila) => OpcionCatalogo(
            id: _entero(fila['id']),
            descripcion: _texto(fila['descripcion']),
          ),
        )
        .toList();
  }

  Future<List<OpcionCatalogo>> conceptos() async {
    final filas = await base.consultar(
      'SELECT id, descripcion FROM conceptos ORDER BY descripcion',
    );
    return filas
        .map(
          (fila) => OpcionCatalogo(
            id: _entero(fila['id']),
            descripcion: _texto(fila['descripcion']),
          ),
        )
        .toList();
  }

  Future<List<OpcionCatalogo>> unidadesMedida() async {
    final filas = await base.consultar(
      'SELECT id, descripcion FROM unidades_medida ORDER BY descripcion',
    );
    return filas
        .map(
          (fila) => OpcionCatalogo(
            id: _entero(fila['id']),
            descripcion: _texto(fila['descripcion']),
          ),
        )
        .toList();
  }

  Future<List<InsumoOpcion>> buscarInsumos(
    String texto, {
    int limite = 40,
  }) async {
    final busqueda = texto.trim();
    if (busqueda.length < 2) {
      return const [];
    }
    final patron = '%${busqueda.replaceAll('%', '')}%';
    final filas = await base.consultar(
      '''
SELECT id, codigo, descripcion FROM insumos
WHERE descripcion LIKE ? OR codigo LIKE ?
ORDER BY descripcion
LIMIT ?
''',
      [patron, patron, limite],
    );
    return filas
        .map(
          (fila) => InsumoOpcion(
            id: _entero(fila['id']),
            codigo: fila['codigo']?.toString(),
            descripcion: _texto(fila['descripcion']),
          ),
        )
        .toList();
  }

  Future<List<ItemPresupuestoOpcion>> itemsPresupuesto(
    int construccionId,
  ) async {
    final filas = await base.consultar(
      '''
SELECT id, descripcion, concepto_id, unidad_medida_id, precio
FROM items_presupuesto_acta
WHERE construccion_id = ?
ORDER BY descripcion
''',
      [construccionId],
    );
    return filas
        .map(
          (fila) => ItemPresupuestoOpcion(
            id: _entero(fila['id']),
            descripcion: _texto(fila['descripcion']),
            conceptoId: _entero(fila['concepto_id']),
            unidadMedidaId: _opcional(fila['unidad_medida_id']),
            precio: fila['precio']?.toString(),
          ),
        )
        .toList();
  }

  int _entero(Object? valor) =>
      valor is int ? valor : int.parse(valor.toString());

  int? _opcional(Object? valor) {
    if (valor == null) {
      return null;
    }
    return valor is int ? valor : int.tryParse(valor.toString());
  }

  String _texto(Object? valor) => valor?.toString() ?? '';
}

String nombrePerfil(int tipoUsuarioId) {
  return switch (tipoUsuarioId) {
    2 => 'Arquitecto',
    3 => 'Contratista',
    4 => 'Revisor',
    _ => 'Perfil $tipoUsuarioId',
  };
}
