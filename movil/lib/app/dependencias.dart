import 'dart:io';
import 'dart:math';

import 'package:flutter/services.dart';
import 'package:path_provider/path_provider.dart';

import '../core/api/cliente_dio.dart';
import '../core/api/cliente_movil.dart';
import '../core/api/red.dart';
import '../core/db/importador_paquete.dart';
import '../core/db/pendientes_db.dart';
import '../core/db/repositorio.dart';
import '../core/db/sqlcipher.dart';
import '../sincronizacion/cola_movil.dart';
import '../sincronizacion/sincronizador_movil.dart';
import '../core/seguridad/almacen_flutter.dart';
import '../core/seguridad/almacen_seguro.dart';
import '../core/seguridad/desbloqueador.dart';
import 'configuracion.dart';

class IdentidadDispositivo {
  String id = '';
}

class Dependencias {
  Dependencias({
    required this.configuracion,
    required this.cliente,
    required this.almacen,
    required this.desbloqueador,
    required this.red,
    required this.repositorio,
    required this.importador,
    required this.cola,
    required this.pendientes,
    required this.sincronizador,
    required this.comunes,
    required this.directorio,
    IdentidadDispositivo? identidad,
    DateTime Function()? ahora,
    Random? aleatorio,
  }) : identidad = identidad ?? IdentidadDispositivo(),
       ahora = ahora ?? DateTime.now,
       aleatorio = aleatorio ?? Random.secure();

  final ConfiguracionApp configuracion;
  final IdentidadDispositivo identidad;
  final ClienteMovil cliente;
  final AlmacenSeguro almacen;
  final Desbloqueador desbloqueador;
  final Red red;
  final RepositorioLocal repositorio;
  final ImportadorPaquete importador;
  final ColaMovil cola;
  final PendientesDb pendientes;
  final SincronizadorMovil sincronizador;
  final Set<String> comunes;
  final Future<Directory> Function() directorio;
  final DateTime Function() ahora;
  final Random aleatorio;

  static Future<Dependencias> crear() async {
    final configuracion = ConfiguracionApp.desdeEntorno();
    final almacen = AlmacenFlutter();
    final abrir = AbrirSqlCipher();
    final directorio = getApplicationSupportDirectory;
    final identidad = IdentidadDispositivo();
    final repositorio = RepositorioLocal(directorio: directorio, abrir: abrir);
    final cola = ColaMovil();
    final pendientes = PendientesDb(
      abrir: abrir,
      ruta: () async => repositorio.rutaPendientes(),
    );
    final cliente = ClienteDio(
      configuracion: configuracion,
      dispositivoId: () => identidad.id,
    );
    return Dependencias(
      configuracion: configuracion,
      identidad: identidad,
      cliente: cliente,
      almacen: almacen,
      desbloqueador: DesbloqueadorLocal(),
      red: RedConectividad(),
      repositorio: repositorio,
      importador: ImportadorPaquete(
        cifrador: CifradorSqlCipher(),
        abrir: abrir,
      ),
      cola: cola,
      pendientes: pendientes,
      sincronizador: SincronizadorMovil(
        cliente: cliente,
        cola: cola,
        pendientes: pendientes,
        configuracion: configuracion,
      ),
      comunes: await cargarContrasenasComunes(),
      directorio: directorio,
    );
  }
}

Future<Set<String>> cargarContrasenasComunes() async {
  final texto = await rootBundle.loadString('assets/contrasenas-comunes.txt');
  return texto
      .split('\n')
      .map((linea) => linea.trim().toLowerCase())
      .where((linea) => linea.isNotEmpty)
      .toSet();
}
