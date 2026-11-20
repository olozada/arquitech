import 'package:material_ui/material_ui.dart';

import '../core/errores/mensajes.dart';

// Paleta de las maquetas (docs/Arquitectos_Movil_Pantallas, diseño Arquitech).
const _amarillo = Color(0xFFFFB200);
const _amarilloSuave = Color(0xFFF9EED3);
// Ámbar oscuro para textos de aviso sobre blanco: el amarillo de marca no
// llega al contraste AA como color de texto.
const _ambarOscuro = Color(0xFF8A5A00);
const _tinta = Color(0xFF1A1A1A);
const _grisTexto = Color(0xFF6F6F6F);
const _grisBorde = Color(0xFFDADADA);
const _grisRelleno = Color(0xFFF4F4F4);
const _fondo = Color(0xFFF8F8F8);
const _oscuro = Color(0xFF2B2B2B);
const _rojo = Color(0xFFCB2424);

/// Radio de campos de texto y desplegables (píldora).
const radioCampo = BorderRadius.all(Radius.circular(28));

/// Radio de tarjetas y bloques.
const radioTarjeta = BorderRadius.all(Radius.circular(16));

/// Radio de botones pequeños con contorno (PDF, XLS).
const radioPequeno = BorderRadius.all(Radius.circular(8));

/// Colores de marca que no tienen rol en [ColorScheme].
@immutable
class ColoresMarca extends ThemeExtension<ColoresMarca> {
  const ColoresMarca({
    required this.exito,
    required this.enLinea,
    required this.cabecera,
    required this.relleno,
  });

  /// Insignias «Abierta» y «Aprobado». Texto blanco encima (contraste AA).
  final Color exito;

  /// Punto de estado «En línea».
  final Color enLinea;

  /// Fondo oscuro de las pantallas de acceso, detrás del logo.
  final Color cabecera;

  /// Relleno de campos en hojas blancas y círculos de íconos.
  final Color relleno;

  static const predeterminados = ColoresMarca(
    exito: Color(0xFF2E7D32),
    enLinea: Color(0xFF0FB91C),
    cabecera: Color(0xFF1E1E1E),
    relleno: _grisRelleno,
  );

  static ColoresMarca de(BuildContext context) =>
      Theme.of(context).extension<ColoresMarca>() ?? predeterminados;

  @override
  ColoresMarca copyWith({
    Color? exito,
    Color? enLinea,
    Color? cabecera,
    Color? relleno,
  }) {
    return ColoresMarca(
      exito: exito ?? this.exito,
      enLinea: enLinea ?? this.enLinea,
      cabecera: cabecera ?? this.cabecera,
      relleno: relleno ?? this.relleno,
    );
  }

  @override
  ColoresMarca lerp(ColoresMarca? other, double t) {
    if (other == null) {
      return this;
    }
    return ColoresMarca(
      exito: Color.lerp(exito, other.exito, t)!,
      enLinea: Color.lerp(enLinea, other.enLinea, t)!,
      cabecera: Color.lerp(cabecera, other.cabecera, t)!,
      relleno: Color.lerp(relleno, other.relleno, t)!,
    );
  }
}

ThemeData temaArquitectos() {
  final esquema = ColorScheme.fromSeed(seedColor: _amarillo).copyWith(
    primary: _amarillo,
    onPrimary: _tinta,
    primaryContainer: _amarilloSuave,
    onPrimaryContainer: _tinta,
    secondary: _oscuro,
    onSecondary: Colors.white,
    secondaryContainer: _amarilloSuave,
    onSecondaryContainer: _tinta,
    tertiary: _ambarOscuro,
    onTertiary: Colors.white,
    error: _rojo,
    onError: Colors.white,
    surface: Colors.white,
    onSurface: _tinta,
    onSurfaceVariant: _grisTexto,
    outline: _grisTexto,
    outlineVariant: _grisBorde,
    inverseSurface: _oscuro,
    onInverseSurface: Colors.white,
    surfaceContainerLowest: Colors.white,
    surfaceContainerLow: _fondo,
    surfaceContainer: _fondo,
    surfaceContainerHigh: Colors.white,
    surfaceContainerHighest: _grisRelleno,
  );
  // Poppins va empaquetada en assets/fuentes (ver pubspec.yaml).
  final base = ThemeData(
    colorScheme: esquema,
    useMaterial3: true,
    fontFamily: 'Poppins',
  );
  final textos = base.textTheme;
  final etiqueta = textos.labelLarge!;
  const pildora = StadiumBorder();

  // Botón primario: píldora amarilla con texto oscuro en negrita.
  final primario = FilledButton.styleFrom(
    backgroundColor: esquema.primary,
    foregroundColor: esquema.onPrimary,
    disabledBackgroundColor: _grisRelleno,
    disabledForegroundColor: _grisTexto,
    minimumSize: const Size.fromHeight(48),
    shape: pildora,
    textStyle: etiqueta.copyWith(fontSize: 16, fontWeight: FontWeight.w700),
  );

  // Botón secundario: píldora blanca con borde gris claro y texto oscuro.
  final secundario =
      OutlinedButton.styleFrom(
        backgroundColor: esquema.surface,
        foregroundColor: esquema.onSurface,
        minimumSize: const Size.fromHeight(48),
        shape: pildora,
        textStyle: etiqueta.copyWith(fontSize: 15, fontWeight: FontWeight.w500),
      ).copyWith(
        side: WidgetStatePropertyAll(BorderSide(color: esquema.outlineVariant)),
      );

  return base.copyWith(
    scaffoldBackgroundColor: _fondo,
    extensions: const [ColoresMarca.predeterminados],
    appBarTheme: AppBarTheme(
      backgroundColor: _fondo,
      foregroundColor: esquema.onSurface,
      elevation: 0,
      scrolledUnderElevation: 0,
      centerTitle: false,
      titleTextStyle: textos.titleLarge?.copyWith(
        color: esquema.onSurface,
        fontSize: 22,
        fontWeight: FontWeight.w600,
      ),
      shape: Border(bottom: BorderSide(color: esquema.outlineVariant)),
    ),
    filledButtonTheme: FilledButtonThemeData(style: primario),
    outlinedButtonTheme: OutlinedButtonThemeData(style: secundario),
    textButtonTheme: TextButtonThemeData(
      style: TextButton.styleFrom(
        foregroundColor: esquema.onSurface,
        textStyle: etiqueta.copyWith(fontSize: 15, fontWeight: FontWeight.w600),
      ),
    ),
    iconButtonTheme: IconButtonThemeData(
      style: IconButton.styleFrom(foregroundColor: esquema.onSurface),
    ),
    floatingActionButtonTheme: FloatingActionButtonThemeData(
      backgroundColor: esquema.primary,
      foregroundColor: esquema.onPrimary,
      elevation: 2,
      focusElevation: 2,
      hoverElevation: 3,
      highlightElevation: 2,
      shape: pildora,
      extendedTextStyle: etiqueta.copyWith(fontWeight: FontWeight.w700),
    ),
    segmentedButtonTheme: SegmentedButtonThemeData(
      style: ButtonStyle(
        backgroundColor: WidgetStateProperty.resolveWith(
          (estados) => estados.contains(WidgetState.selected)
              ? esquema.primary
              : esquema.surface,
        ),
        foregroundColor: WidgetStatePropertyAll(esquema.onSurface),
        side: WidgetStatePropertyAll(BorderSide(color: esquema.outlineVariant)),
      ),
    ),
    inputDecorationTheme: camposContorno(esquema),
    // El amarillo no se ve como cursor sobre blanco: cursor oscuro y
    // selección en amarillo suave.
    textSelectionTheme: TextSelectionThemeData(
      cursorColor: esquema.onSurface,
      selectionColor: esquema.primary.withValues(alpha: 0.4),
      selectionHandleColor: esquema.primary,
    ),
    cardTheme: CardThemeData(
      color: esquema.surface,
      elevation: 0,
      margin: const EdgeInsets.symmetric(vertical: 6),
      shape: RoundedRectangleBorder(
        borderRadius: radioTarjeta,
        side: BorderSide(color: esquema.outlineVariant),
      ),
    ),
    dividerTheme: DividerThemeData(
      color: esquema.outlineVariant,
      space: 1,
      thickness: 1,
    ),
    switchTheme: SwitchThemeData(
      thumbColor: const WidgetStatePropertyAll(Colors.white),
      trackColor: WidgetStateProperty.resolveWith(
        (estados) => estados.contains(WidgetState.selected)
            ? esquema.primary
            : esquema.outlineVariant,
      ),
      trackOutlineColor: const WidgetStatePropertyAll(Colors.transparent),
    ),
    tabBarTheme: TabBarThemeData(
      labelColor: esquema.onSurface,
      unselectedLabelColor: esquema.onSurfaceVariant,
      labelStyle: textos.titleSmall?.copyWith(fontWeight: FontWeight.w700),
      unselectedLabelStyle: textos.titleSmall?.copyWith(
        fontWeight: FontWeight.w400,
      ),
      indicatorColor: esquema.primary,
      indicatorSize: TabBarIndicatorSize.tab,
      // La línea gris la pone el borde inferior de la barra superior.
      dividerHeight: 0,
    ),
    dialogTheme: DialogThemeData(
      backgroundColor: esquema.surface,
      surfaceTintColor: Colors.transparent,
      shape: const RoundedRectangleBorder(borderRadius: radioTarjeta),
    ),
    popupMenuTheme: PopupMenuThemeData(
      color: esquema.surface,
      surfaceTintColor: Colors.transparent,
      shape: RoundedRectangleBorder(
        borderRadius: radioTarjeta,
        side: BorderSide(color: esquema.outlineVariant),
      ),
    ),
    progressIndicatorTheme: ProgressIndicatorThemeData(
      color: esquema.primary,
      linearTrackColor: _grisRelleno,
    ),
  );
}

/// Campos blancos en píldora con borde gris claro (formularios y buscadores
/// sobre el fondo gris de la app). Es el estilo por defecto del tema.
InputDecorationThemeData camposContorno(ColorScheme esquema) {
  OutlineInputBorder borde(Color color, [double ancho = 1]) =>
      OutlineInputBorder(
        borderRadius: radioCampo,
        borderSide: BorderSide(color: color, width: ancho),
      );
  return InputDecorationThemeData(
    constraints: const BoxConstraints(minHeight: 48),
    contentPadding: const EdgeInsets.symmetric(horizontal: 20, vertical: 14),
    filled: true,
    fillColor: esquema.surface,
    border: borde(esquema.outlineVariant),
    enabledBorder: borde(esquema.outlineVariant),
    disabledBorder: borde(esquema.outlineVariant.withValues(alpha: 0.5)),
    focusedBorder: borde(esquema.onSurface, 1.5),
    errorBorder: borde(esquema.error),
    focusedErrorBorder: borde(esquema.error, 1.5),
    hintStyle: TextStyle(color: esquema.onSurfaceVariant, fontSize: 14),
  );
}

/// Campos rellenos de gris y sin borde, para las hojas blancas de las
/// pantallas de acceso (login, desbloqueo, cambio de contraseña).
InputDecorationThemeData camposRellenos(ColorScheme esquema, Color relleno) {
  const sinBorde = OutlineInputBorder(
    borderRadius: radioCampo,
    borderSide: BorderSide.none,
  );
  return camposContorno(esquema).copyWith(
    fillColor: relleno,
    border: sinBorde,
    enabledBorder: sinBorde,
    disabledBorder: sinBorde,
  );
}

/// Botón pequeño con contorno gris (PDF, XLS). Solo se muestra con conexión;
/// quien lo usa decide si va o no.
class BotonBarra extends StatelessWidget {
  const BotonBarra({
    required this.texto,
    required this.descripcion,
    required this.onPressed,
    this.icono,
    super.key,
  });

  final String texto;
  final String descripcion;
  final VoidCallback? onPressed;
  final IconData? icono;

  @override
  Widget build(BuildContext context) {
    final tema = Theme.of(context);
    final esquema = tema.colorScheme;
    // El tema da a OutlinedButton ancho mínimo infinito; aquí se usa el ancho
    // del texto.
    final estilo = OutlinedButton.styleFrom(
      minimumSize: const Size(48, 36),
      padding: const EdgeInsets.symmetric(horizontal: 12),
      shape: const RoundedRectangleBorder(borderRadius: radioPequeno),
      foregroundColor: esquema.onSurface,
      side: BorderSide(color: esquema.outlineVariant),
      textStyle: tema.textTheme.labelLarge?.copyWith(
        fontWeight: FontWeight.w500,
      ),
    );
    return Padding(
      padding: const EdgeInsetsDirectional.only(end: 8),
      child: Tooltip(
        message: descripcion,
        child: icono == null
            ? OutlinedButton(
                style: estilo,
                onPressed: onPressed,
                child: Text(texto),
              )
            : OutlinedButton.icon(
                style: estilo,
                onPressed: onPressed,
                icon: Icon(icono, size: 18),
                label: Text(texto),
              ),
      ),
    );
  }
}

/// Insignia en píldora para estados: «Abierta», «Cerrada», «Aprobado»…
class EtiquetaEstado extends StatelessWidget {
  const EtiquetaEstado({
    required this.texto,
    required this.fondo,
    required this.frente,
    this.icono,
    super.key,
  });

  final String texto;
  final Color fondo;
  final Color frente;
  final IconData? icono;

  @override
  Widget build(BuildContext context) {
    final estilo = Theme.of(context).textTheme.labelSmall
        ?.copyWith(color: frente, fontWeight: FontWeight.w600);
    return DecoratedBox(
      decoration: ShapeDecoration(color: fondo, shape: const StadiumBorder()),
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 3),
        child: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            if (icono != null) ...[
              Icon(icono, size: 12, color: frente),
              const SizedBox(width: 4),
            ],
            Flexible(
              child: Text(
                texto,
                style: estilo,
                overflow: TextOverflow.ellipsis,
              ),
            ),
          ],
        ),
      ),
    );
  }
}

/// Círculo con las iniciales del usuario en la barra superior. Lleva a la
/// cuenta.
class AvatarUsuario extends StatelessWidget {
  const AvatarUsuario({
    required this.nombre,
    required this.onPressed,
    super.key,
  });

  final String? nombre;
  final VoidCallback onPressed;

  static String iniciales(String? nombre) {
    final partes = (nombre ?? '')
        .trim()
        .split(RegExp(r'\s+'))
        .where((parte) => parte.isNotEmpty)
        .toList();
    if (partes.isEmpty) {
      return '?';
    }
    final primera = partes.first.characters.first;
    final ultima = partes.length > 1 ? partes.last.characters.first : '';
    return '$primera$ultima'.toUpperCase();
  }

  @override
  Widget build(BuildContext context) {
    final tema = Theme.of(context);
    final esquema = tema.colorScheme;
    return Padding(
      padding: const EdgeInsetsDirectional.only(end: 12),
      child: IconButton(
        tooltip: 'Cuenta',
        onPressed: onPressed,
        icon: CircleAvatar(
          radius: 20,
          backgroundColor: esquema.primaryContainer,
          foregroundColor: esquema.onPrimaryContainer,
          child: Text(
            iniciales(nombre),
            style: tema.textTheme.labelLarge?.copyWith(
              fontWeight: FontWeight.w700,
            ),
          ),
        ),
      ),
    );
  }
}

class AvisoTexto extends StatelessWidget {
  const AvisoTexto(this.texto, {super.key});

  final String? texto;

  @override
  Widget build(BuildContext context) {
    if (texto == null || texto!.isEmpty) {
      return const SizedBox.shrink();
    }
    final esquema = Theme.of(context).colorScheme;
    // El único aviso de éxito es «Datos actualizados»; el resto son errores.
    final exito = texto == Mensajes.datosActualizados;
    final fondo = exito ? esquema.primaryContainer : esquema.errorContainer;
    final frente = exito
        ? esquema.onPrimaryContainer
        : esquema.onErrorContainer;
    return Padding(
      padding: const EdgeInsets.only(bottom: 12),
      child: Semantics(
        liveRegion: true,
        child: DecoratedBox(
          decoration: BoxDecoration(color: fondo, borderRadius: radioTarjeta),
          child: Padding(
            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Icon(
                  exito ? Icons.check_circle_outline : Icons.error_outline,
                  size: 20,
                  color: frente,
                ),
                const SizedBox(width: 8),
                Expanded(
                  child: Text(
                    texto!,
                    key: const Key('aviso'),
                    style: TextStyle(color: frente),
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
