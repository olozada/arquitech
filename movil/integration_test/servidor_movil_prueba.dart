import 'dart:convert';
import 'dart:io';

import 'package:arquitectos_movil/core/api/cliente_movil.dart';
import 'package:arquitectos_movil/core/api/version_app_movil.dart';
import 'package:arquitectos_movil/sincronizacion/modelos.dart';

import 'simulador_api_movil.dart';

const _versionMinimaApp = '1.0.0';

/// Servidor HTTP opcional (E2E con dispositivo y `HttpOverrides`).
class ServidorMovilPrueba {
  final SimuladorApiMovil _simulador = SimuladorApiMovil();
  HttpServer? _servidor;

  String get url => 'http://127.0.0.1:${_servidor!.port}';

  Future<void> iniciar() async {
    await _simulador.iniciar();
    _servidor = await HttpServer.bind(InternetAddress.loopbackIPv4, 0);
    _servidor!.listen(_atender);
  }

  Future<void> cerrar() async {
    await _servidor?.close(force: true);
    _servidor = null;
  }

  Future<void> _atender(HttpRequest peticion) async {
    if (_requiereVersion(peticion)) {
      final anunciada = peticion.headers.value('x-version-app') ?? '';
      if (versionAppEsMenor(anunciada, _versionMinimaApp)) {
        peticion.response.statusCode = HttpStatus.upgradeRequired;
        await peticion.response.close();
        return;
      }
    }
    final metodo = peticion.method;
    final ruta = peticion.uri.path;
    if (metodo == 'POST' && ruta == '/api/auth/login') {
      final cuerpoLogin = await utf8.decoder.bind(peticion).join();
      final loginMapa = jsonDecode(cuerpoLogin) as Map<String, Object?>;
      final usuario = loginMapa['usuario'] as String? ?? 'carlos';
      final cred = _simulador.login(usuario);
      final acceso = _jwt(cred);
      peticion.response.headers
        ..add('set-cookie', 'arquitectos.acceso=$acceso; path=/api; httponly')
        ..add(
          'set-cookie',
          'arquitectos.renovacion=${cred.refreshToken}; path=/api/auth/renovar; httponly',
        );
      await _json(peticion, 200, {
        'nombre': cred.usuario.nombre,
        'tipoUsuario': cred.usuario.tipoUsuarioId == 4
            ? 'Revisor'
            : 'Contratista',
        'debeCambiarContrasena': cred.debeCambiarContrasena,
      });
      return;
    }
    if (metodo == 'GET' && ruta == '/api/movil/v1/paquete') {
      peticion.response.statusCode = HttpStatus.ok;
      peticion.response.headers
        ..set('content-type', 'application/gzip')
        ..set('x-paquete-sha256', _simulador.sha256Paquete)
        ..set('x-paquete-esquema', '1')
        ..set('x-paquete-generado', _simulador.generadoPaquete);
      peticion.response.add(_simulador.bytesGzip());
      await peticion.response.close();
      return;
    }
    if (metodo == 'POST' && ruta == '/api/movil/v1/sincronizar') {
      final cuerpo = await utf8.decoder.bind(peticion).join();
      final mapa = jsonDecode(cuerpo) as Map<String, Object?>;
      final ops = _leerOperaciones(mapa['operaciones']);
      final resp = await _simulador.sincronizar(ops);
      await _json(peticion, 200, {
        'resultados': resp.resultados
            .map(
              (r) => {
                'idOperacion': r.idOperacion,
                'estado': r.estado,
                if (r.lineaId != null) 'lineaId': r.lineaId,
                if (r.idCliente != null) 'idCliente': r.idCliente,
                if (r.rowVersion != null) 'rowVersion': r.rowVersion,
                if (r.mensaje != null) 'mensaje': r.mensaje,
              },
            )
            .toList(),
        'requiereNuevoPaquete': resp.requiereNuevoPaquete,
      });
      return;
    }
    peticion.response.statusCode = HttpStatus.notFound;
    await peticion.response.close();
  }

  bool _requiereVersion(HttpRequest peticion) {
    final ruta = peticion.uri.path;
    if (ruta == '/api/movil/v1/paquete' ||
        ruta == '/api/movil/v1/sincronizar') {
      return true;
    }
    return false;
  }

  List<OperacionSalida> _leerOperaciones(Object? lista) {
    if (lista is! List) {
      return const [];
    }
    return lista.map((e) {
      final mapa = Map<String, Object?>.from(e! as Map);
      return OperacionSalida(
        idOperacion: mapa['idOperacion']?.toString() ?? '',
        secuencia: (mapa['secuencia'] as num).toInt(),
        tipo: mapa['tipo']?.toString() ?? '',
        numeroActa: (mapa['numeroActa'] as num).toInt(),
        creadaEn: mapa['creadaEn']?.toString() ?? '',
        idCliente: mapa['idCliente']?.toString(),
        linea: mapa['linea'] is Map
            ? LineaSincronizacion.desdeJson(
                Map<String, Object?>.from(mapa['linea']! as Map),
              )
            : null,
      );
    }).toList();
  }

  String _jwt(Credenciales cred) {
    String parte(Object valor) {
      return base64Url
          .encode(utf8.encode(jsonEncode(valor)))
          .replaceAll('=', '');
    }

    final exp = cred.expiraEn.millisecondsSinceEpoch ~/ 1000;
    return '${parte({'alg': 'none'})}.${parte({'sub': '${cred.usuario.id}', 'tipoUsuarioId': '${cred.usuario.tipoUsuarioId}', 'exp': exp})}.firma';
  }

  Future<void> _json(HttpRequest peticion, int estado, Object cuerpo) async {
    peticion.response.statusCode = estado;
    peticion.response.headers.contentType = ContentType.json;
    peticion.response.write(jsonEncode(cuerpo));
    await peticion.response.close();
  }
}
