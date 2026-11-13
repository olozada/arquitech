import 'dart:convert';
import 'dart:math';

import '../app/configuracion.dart';
import '../core/api/cliente_movil.dart';
import '../core/api/errores.dart';
import '../core/db/base_local.dart';
import '../core/db/pendientes_db.dart';
import 'cola_movil.dart';
import 'modelos.dart';

class SincronizadorMovil {
  SincronizadorMovil({
    required this.cliente,
    required this.cola,
    required this.pendientes,
    required this.configuracion,
    DateTime Function()? ahora,
  }) : _ahora = ahora ?? DateTime.now;

  final ClienteMovil cliente;
  final ColaMovil cola;
  final PendientesDb pendientes;
  final ConfiguracionApp configuracion;
  final DateTime Function() _ahora;

  Future<bool> subirLote({
    required BaseLocal base,
    required String token,
    required String dispositivoId,
  }) async {
    final pendientesCola = await cola.listarPendientes(base);
    if (pendientesCola.isEmpty) {
      return false;
    }

    final tamano = min(pendientesCola.length, configuracion.maxOperacionesLote);
    final lote = pendientesCola.take(tamano).toList();
    await cola.marcarEnviando(base, lote.map((fila) => fila.idOperacion));

    var reintentos = 0;
    while (true) {
      try {
        final respuesta = await cliente.sincronizar(
          token: token,
          dispositivoId: dispositivoId,
          dispositivoUuid: dispositivoId,
          versionEsquema: configuracion.versionEsquema,
          operaciones: lote.map((fila) => fila.aOperacion()).toList(),
        );
        await _procesarResultados(base, lote, respuesta);
        return respuesta.requiereNuevoPaquete;
      } on DispositivoRevocado {
        rethrow;
      } on ActualizacionRequerida {
        rethrow;
      } on ErrorServidor catch (error) {
        if (error.estado != null && error.estado! >= 500 && reintentos < 5) {
          reintentos += 1;
          final espera = min(300000, 1000 * (1 << reintentos));
          await Future<void>.delayed(Duration(milliseconds: espera));
          continue;
        }
        await cola.devolverAPendiente(
          base,
          lote.map((fila) => fila.idOperacion),
        );
        rethrow;
      } catch (error) {
        if (reintentos < 5) {
          reintentos += 1;
          final espera = min(300000, 1000 * (1 << reintentos));
          await Future<void>.delayed(Duration(milliseconds: espera));
          continue;
        }
        await cola.devolverAPendiente(
          base,
          lote.map((fila) => fila.idOperacion),
        );
        rethrow;
      }
    }
  }

  Future<void> _procesarResultados(
    BaseLocal base,
    List<FilaCola> lote,
    RespuestaSincronizacion respuesta,
  ) async {
    final porId = {
      for (final item in respuesta.resultados) item.idOperacion: item,
    };
    final instante = _ahora().toUtc().toIso8601String();
    for (final fila in lote) {
      final resultado = porId[fila.idOperacion];
      if (resultado == null) {
        await cola.devolverAPendiente(base, [fila.idOperacion]);
        continue;
      }
      switch (resultado.estado) {
        case 'Aplicada':
          await cola.quitarOperacion(base, fila.idOperacion);
          await _aplicarEnLocal(base, fila, resultado);
        case 'Rechazada':
        case 'Conflicto':
          await cola.quitarOperacion(base, fila.idOperacion);
          await pendientes.insertar(
            OperacionFallidaRegistro(
              idOperacion: fila.idOperacion,
              secuencia: fila.secuencia,
              tipo: fila.tipo,
              numeroActa: fila.numeroActa,
              localId: fila.localId,
              estado: resultado.estado,
              mensaje: resultado.mensaje ?? '',
              payloadJson: fila.payloadJson,
              resultadoJson: jsonEncode({
                'idOperacion': resultado.idOperacion,
                'estado': resultado.estado,
                'mensaje': resultado.mensaje,
                'lineaServidor': resultado.lineaServidor,
              }),
              registradaEn: instante,
            ),
          );
          if (resultado.estado == 'Conflicto' &&
              resultado.lineaServidor != null) {
            await _reemplazarConServidor(base, fila.localId, resultado);
          }
        default:
          await cola.devolverAPendiente(base, [fila.idOperacion]);
      }
    }
  }

  Future<void> _aplicarEnLocal(
    BaseLocal base,
    FilaCola fila,
    ResultadoOperacion resultado,
  ) async {
    final localId = fila.localId;
    if (localId == null) {
      return;
    }
    if (fila.tipo == 'EliminarLinea') {
      await base.ejecutar('DELETE FROM acta_detalles WHERE local_id = ?', [
        localId,
      ]);
      return;
    }
    if (fila.tipo == 'AgregarLinea' && resultado.lineaId != null) {
      await base.ejecutar(
        '''
UPDATE acta_detalles SET id = ?, id_cliente = ?, row_version = ?, estado_local = 'sincronizado'
WHERE local_id = ?
''',
        [resultado.lineaId, resultado.idCliente, resultado.rowVersion, localId],
      );
      return;
    }
    if (fila.tipo == 'EditarLinea' || fila.tipo == 'RevisarLinea') {
      await base.ejecutar(
        "UPDATE acta_detalles SET row_version = ?, estado_local = 'sincronizado' WHERE local_id = ?",
        [resultado.rowVersion, localId],
      );
    }
  }

  Future<void> _reemplazarConServidor(
    BaseLocal base,
    String? localId,
    ResultadoOperacion resultado,
  ) async {
    if (localId == null || resultado.lineaServidor == null) {
      return;
    }
    final linea = resultado.lineaServidor!;
    await base.ejecutar(
      '''
UPDATE acta_detalles SET id = ?, construccion_id = ?, concepto_id = ?, unidad_medida_id = ?, insumo_id = ?,
  presupuesto_item_id = ?, cantidad = ?, precio = ?, subtotal = ?, descripcion = ?, tipo_usuario_id = ?,
  aprobado = ?, row_version = ?, estado_local = 'sincronizado'
WHERE local_id = ?
''',
      [
        linea['id'],
        linea['construccionId'],
        linea['conceptoId'],
        linea['unidadMedidaId'],
        linea['insumoId'],
        linea['presupuestoItemId'],
        linea['cantidad'],
        linea['precio'],
        linea['subtotal'],
        linea['descripcion'],
        linea['tipoUsuarioId'],
        (linea['aprobado'] == true) ? 1 : 0,
        linea['rowVersion'],
        localId,
      ],
    );
  }
}
