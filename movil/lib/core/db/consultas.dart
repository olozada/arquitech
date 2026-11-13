import 'package:decimal/decimal.dart';

import 'base_local.dart';

part 'consultas_actas.dart';
part 'consultas_catalogo.dart';

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

abstract class ConsultasBase {
  BaseLocal get base;

  int _entero(Object? valor);
  int? _opcional(Object? valor);
  String _texto(Object? valor);
}

class ConsultasLocales extends ConsultasBase
    with ConsultasCatalogo, ConsultasActas {
  ConsultasLocales(this.base);

  @override
  final BaseLocal base;

  @override
  int _entero(Object? valor) =>
      valor is int ? valor : int.parse(valor.toString());

  @override
  int? _opcional(Object? valor) {
    if (valor == null) {
      return null;
    }
    return valor is int ? valor : int.tryParse(valor.toString());
  }

  @override
  String _texto(Object? valor) => valor?.toString() ?? '';

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
    final condiciones = <String>['1=1'];
    final parametros = <Object?>[];
    switch (filtro.estado) {
      case EstadoActaFiltro.abiertas:
        condiciones.add('a.cerrada = 0');
      case EstadoActaFiltro.cerradas:
        condiciones.add('a.cerrada = 1');
      case EstadoActaFiltro.todas:
        break;
    }
    if (filtro.desde != null) {
      condiciones.add('a.fecha_final >= ?');
      parametros.add(filtro.desde);
    }
    if (filtro.hasta != null) {
      condiciones.add('a.fecha_inicial <= ?');
      parametros.add(filtro.hasta);
    }
    final busqueda = filtro.texto.trim().toLowerCase();
    if (busqueda.isNotEmpty) {
      condiciones.add('''
(lower(cast(a.numero_acta as text)) || ' ' || lower(ar.nombre) || ' ' || lower(re.nombre) || ' ' || lower(co.nombre))
 LIKE ?
''');
      parametros.add('%$busqueda%');
    }
    if (filtro.obraId != null) {
      condiciones.add('''
EXISTS (
  SELECT 1 FROM acta_detalles d
  WHERE d.numero_acta = a.numero_acta
    AND d.estado_local != 'eliminado'
    AND d.construccion_id = ?
)
''');
      parametros.add(filtro.obraId);
    }
    final ordenSql = switch (filtro.orden) {
      OrdenActas.fechaInicial => 'a.fecha_inicial DESC',
      OrdenActas.fechaFinal => 'a.fecha_final DESC',
      OrdenActas.numero => 'a.numero_acta DESC',
    };

    final filas = await base.consultar('''
SELECT a.numero_acta, a.fecha_inicial, a.fecha_final, a.cerrada,
       ar.nombre AS arquitecto, re.nombre AS revisor, co.nombre AS contratista,
       EXISTS(
         SELECT 1 FROM outbox o
         WHERE o.numero_acta = a.numero_acta
           AND o.estado IN ('pendiente', 'enviando')
       ) AS en_cola,
       EXISTS(
         SELECT 1 FROM acta_detalles dl
         WHERE dl.numero_acta = a.numero_acta
           AND dl.estado_local != 'sincronizado'
           AND dl.estado_local != 'eliminado'
       ) AS cambios_local
FROM actas a
JOIN arquitectos ar ON ar.id = a.arquitecto_id
JOIN revisores re ON re.id = a.revisor_id
JOIN contratistas co ON co.id = a.contratista_id
WHERE ${condiciones.join(' AND ')}
ORDER BY $ordenSql
''', parametros);

    if (filas.isEmpty) {
      return const [];
    }

    final numeros = filas.map((f) => _entero(f['numero_acta'])).toList();
    final marcadores = List.filled(numeros.length, '?').join(',');
    final subtotales = await base.consultar('''
SELECT numero_acta, subtotal
FROM acta_detalles
WHERE numero_acta IN ($marcadores)
  AND aprobado = 1
  AND estado_local != 'eliminado'
''', numeros);
    final totales = <int, Decimal>{};
    for (final fila in subtotales) {
      final numero = _entero(fila['numero_acta']);
      final parcial = Decimal.parse(_texto(fila['subtotal']));
      totales[numero] = (totales[numero] ?? Decimal.zero) + parcial;
    }

    return [
      for (final fila in filas)
        ActaResumen(
          numero: _entero(fila['numero_acta']),
          fechaInicial: _texto(fila['fecha_inicial']),
          fechaFinal: _texto(fila['fecha_final']),
          arquitecto: _texto(fila['arquitecto']),
          revisor: _texto(fila['revisor']),
          contratista: _texto(fila['contratista']),
          cerrada: _entero(fila['cerrada']) == 1,
          totalAprobado: totales[_entero(fila['numero_acta'])] ?? Decimal.zero,
          cambiosSinSincronizar:
              _entero(fila['en_cola']) == 1 ||
              _entero(fila['cambios_local']) == 1,
        ),
    ];
  }
}

String nombrePerfil(int tipoUsuarioId) {
  return switch (tipoUsuarioId) {
    2 => 'Arquitecto',
    3 => 'Contratista',
    4 => 'Revisor',
    _ => 'Perfil $tipoUsuarioId',
  };
}
