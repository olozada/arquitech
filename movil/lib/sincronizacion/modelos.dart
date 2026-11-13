class LineaSincronizacion {
  const LineaSincronizacion({
    required this.construccionId,
    required this.conceptoId,
    this.unidadMedidaId,
    this.insumoId,
    this.presupuestoItemId,
    required this.cantidad,
    required this.precio,
    required this.descripcion,
  });

  final int construccionId;
  final int conceptoId;
  final int? unidadMedidaId;
  final int? insumoId;
  final int? presupuestoItemId;
  final String cantidad;
  final String precio;
  final String descripcion;

  Map<String, Object?> aJson() => {
    'construccionId': construccionId,
    'conceptoId': conceptoId,
    if (unidadMedidaId != null) 'unidadMedidaId': unidadMedidaId,
    if (insumoId != null) 'insumoId': insumoId,
    if (presupuestoItemId != null) 'presupuestoItemId': presupuestoItemId,
    'cantidad': cantidad,
    'precio': precio,
    'descripcion': descripcion,
  };

  factory LineaSincronizacion.desdeJson(Map<String, Object?> mapa) {
    return LineaSincronizacion(
      construccionId: _entero(mapa['construccionId']),
      conceptoId: _entero(mapa['conceptoId']),
      unidadMedidaId: _opcional(mapa['unidadMedidaId']),
      insumoId: _opcional(mapa['insumoId']),
      presupuestoItemId: _opcional(mapa['presupuestoItemId']),
      cantidad: mapa['cantidad']?.toString() ?? '0',
      precio: mapa['precio']?.toString() ?? '0',
      descripcion: mapa['descripcion']?.toString() ?? '',
    );
  }

  static int _entero(Object? valor) =>
      valor is int ? valor : int.parse(valor.toString());

  static int? _opcional(Object? valor) {
    if (valor == null) {
      return null;
    }
    return valor is int ? valor : int.tryParse(valor.toString());
  }
}

class OperacionSalida {
  const OperacionSalida({
    required this.idOperacion,
    required this.secuencia,
    required this.tipo,
    required this.numeroActa,
    required this.creadaEn,
    this.idCliente,
    this.lineaId,
    this.rowVersionLinea,
    this.aprobado,
    this.linea,
  });

  final String idOperacion;
  final int secuencia;
  final String tipo;
  final int numeroActa;
  final String creadaEn;
  final String? idCliente;
  final int? lineaId;
  final String? rowVersionLinea;
  final bool? aprobado;
  final LineaSincronizacion? linea;

  Map<String, Object?> aSolicitud() => {
    'idOperacion': idOperacion,
    'secuencia': secuencia,
    'tipo': tipo,
    'numeroActa': numeroActa,
    'creadaEn': creadaEn,
    if (idCliente != null) 'idCliente': idCliente,
    if (lineaId != null) 'lineaId': lineaId,
    if (rowVersionLinea != null) 'rowVersionLinea': rowVersionLinea,
    if (aprobado != null) 'aprobado': aprobado,
    if (linea != null) 'linea': linea!.aJson(),
  };
}

class ResultadoOperacion {
  const ResultadoOperacion({
    required this.idOperacion,
    required this.estado,
    this.lineaId,
    this.idCliente,
    this.rowVersion,
    this.mensaje,
    this.lineaServidor,
  });

  final String idOperacion;
  final String estado;
  final int? lineaId;
  final String? idCliente;
  final String? rowVersion;
  final String? mensaje;
  final Map<String, Object?>? lineaServidor;

  factory ResultadoOperacion.desdeJson(Map<String, Object?> mapa) {
    return ResultadoOperacion(
      idOperacion: mapa['idOperacion']?.toString() ?? '',
      estado: mapa['estado']?.toString() ?? '',
      lineaId: _opcional(mapa['lineaId']),
      idCliente: mapa['idCliente']?.toString(),
      rowVersion: mapa['rowVersion']?.toString(),
      mensaje: mapa['mensaje']?.toString(),
      lineaServidor: mapa['lineaServidor'] is Map
          ? Map<String, Object?>.from(mapa['lineaServidor']! as Map)
          : null,
    );
  }

  static int? _opcional(Object? valor) {
    if (valor == null) {
      return null;
    }
    return valor is int ? valor : int.tryParse(valor.toString());
  }
}

class RespuestaSincronizacion {
  const RespuestaSincronizacion({
    required this.resultados,
    required this.requiereNuevoPaquete,
  });

  final List<ResultadoOperacion> resultados;
  final bool requiereNuevoPaquete;
}

class OperacionFallida {
  const OperacionFallida({
    required this.idOperacion,
    required this.secuencia,
    required this.tipo,
    required this.numeroActa,
    this.localId,
    required this.estado,
    required this.mensaje,
    required this.payloadJson,
    required this.resultadoJson,
    required this.registradaEn,
  });

  final String idOperacion;
  final int secuencia;
  final String tipo;
  final int numeroActa;
  final String? localId;
  final String estado;
  final String mensaje;
  final String payloadJson;
  final String resultadoJson;
  final String registradaEn;
}
