import 'package:decimal/decimal.dart';

import '../decimal/dinero.dart';

/// Mensajes iguales a los del servidor y a la web.
abstract final class Mensajes {
  static const usuario = 'Debe ingresar el usuario.';
  static const contrasena = 'Debe ingresar la contraseña.';
  static const actual = 'Debe ingresar la contraseña actual.';
  static const nueva = 'Debe ingresar la contraseña nueva.';
  static const confirmar = 'Debe confirmar la contraseña.';
  static const noCoinciden = 'Las contraseñas no coinciden.';
  static const distinta = 'La contraseña nueva debe ser distinta de la actual.';
  static const corta = 'La contraseña debe tener al menos 10 caracteres.';
  static const composicion = 'La contraseña debe incluir letras y números.';
  static const igualUsuario = 'La contraseña no puede ser igual al usuario.';
  static const comun = 'La contraseña es demasiado común.';
  static const actualIncorrecta = 'La contraseña actual no es correcta.';
  static const construccion = 'Debe seleccionar la construcción del ítem.';
  static const concepto = 'Debe seleccionar el concepto del ítem.';
  static const unidad = 'Debe seleccionar la unidad de medida del ítem.';
  static const cantidad = 'Debe indicar la cantidad del ítem.';
  static const precio = 'Debe indicar el precio del ítem.';
  static const descripcion = 'Debe indicar la descripción del ítem.';
  static const descripcionLarga =
      'La descripción no puede superar 500 caracteres.';
  static const insumo = 'Debe seleccionar un insumo válido.';
  static const presupuesto =
      'El ítem de presupuesto no pertenece a la construcción o el presupuesto no está aprobado.';
  static const lineaAjena = 'No puede modificar un ítem de otro perfil.';
  static const soloRevisor = 'Solo el revisor puede aprobar o rechazar ítems.';
  static const revisorNoEdita =
      'El revisor no puede modificar ni eliminar ítems.';
  static const conflictoLinea = 'Otra persona modificó esta línea.';
  static const revisorAgrega = 'El revisor no puede agregar ítems.';
  static const cerrada =
      'El acta ya está cerrada. No se pueden agregar, modificar ni eliminar ítems.';
  static const requiereBloqueo =
      'Configure un PIN, patrón o huella en el teléfono para usar Arquitectos.';
  static const sinConexion = 'Sin conexión.';
  static const faltaServidor = 'Falta la dirección del servidor (API_URL).';
  static const actualice = 'Actualice la aplicación.';
  static const datosActualizados = 'Datos actualizados.';
  static const sincronizacionIncompleta =
      'Quedaron cambios sin enviar. Intente de nuevo más tarde.';
  static const reemplazoBloqueado =
      'Hay operaciones pendientes. No se reemplaza la base hasta sincronizarlas.';
  static const paqueteDistinto = 'El paquete tiene un esquema distinto.';
  static const paqueteCorrupto = 'El paquete no coincide con su huella.';
  static const cambioUsuario =
      'Hay operaciones pendientes del usuario anterior. Si continúa, se perderán.';
}

String? errorContrasena(
  String contrasena,
  String? usuario,
  Set<String> comunes,
) {
  if (contrasena.length < 10) {
    return Mensajes.corta;
  }

  final tieneLetra = RegExp(r'\p{L}', unicode: true).hasMatch(contrasena);
  final tieneDigito = RegExp(r'\d').hasMatch(contrasena);
  if (!tieneLetra || !tieneDigito) {
    return Mensajes.composicion;
  }

  final nombre = usuario?.trim();
  if (nombre != null &&
      nombre.isNotEmpty &&
      contrasena.toLowerCase() == nombre.toLowerCase()) {
    return Mensajes.igualUsuario;
  }

  if (comunes.contains(contrasena.toLowerCase())) {
    return Mensajes.comun;
  }

  return null;
}

String? errorCambioContrasena({
  required String actual,
  required String nueva,
  required String confirmacion,
  required String? usuario,
  required Set<String> comunes,
}) {
  if (actual.isEmpty) {
    return Mensajes.actual;
  }
  if (nueva.isEmpty) {
    return Mensajes.nueva;
  }

  final politica = errorContrasena(nueva, usuario, comunes);
  if (politica != null) {
    return politica;
  }
  if (nueva == actual) {
    return Mensajes.distinta;
  }
  if (confirmacion.isEmpty) {
    return Mensajes.confirmar;
  }
  if (nueva != confirmacion) {
    return Mensajes.noCoinciden;
  }
  return null;
}

/// Reglas numéricas de la línea. El catálogo activo lo revalida el servidor.
String? errorCantidad(Decimal? valor) {
  if (valor == null || redondearLejosDeCero(valor, 4) <= Decimal.zero) {
    return Mensajes.cantidad;
  }
  return null;
}

String? errorPrecio(Decimal? valor) {
  if (valor == null || redondearLejosDeCero(valor, 2) < Decimal.zero) {
    return Mensajes.precio;
  }
  return null;
}

String? errorDescripcion(String? valor) {
  final texto = valor?.trim() ?? '';
  if (texto.isEmpty) {
    return Mensajes.descripcion;
  }
  if (texto.length > 500) {
    return Mensajes.descripcionLarga;
  }
  return null;
}
