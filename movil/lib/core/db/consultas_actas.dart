part of 'consultas.dart';

mixin ConsultasActas on ConsultasBase {
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
}
