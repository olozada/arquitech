sealed class FalloMovil implements Exception {
  const FalloMovil(this.mensaje);

  final String mensaje;

  @override
  String toString() => mensaje;
}

class ErrorServidor extends FalloMovil {
  const ErrorServidor(super.mensaje, {this.estado});

  final int? estado;
}

class DispositivoRevocado extends FalloMovil {
  const DispositivoRevocado(super.mensaje);
}

class ActualizacionRequerida extends FalloMovil {
  const ActualizacionRequerida(super.mensaje);
}

class PaqueteInvalido extends FalloMovil {
  const PaqueteInvalido(super.mensaje);
}

class ReemplazoBloqueado extends FalloMovil {
  const ReemplazoBloqueado(super.mensaje);
}
