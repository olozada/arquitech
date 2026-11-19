package com.celuweb.arquitectos_movil

import android.app.KeyguardManager
import android.content.Context
import android.os.Build
import android.os.Bundle
import android.view.WindowManager
import io.flutter.embedding.android.FlutterFragmentActivity
import io.flutter.embedding.engine.FlutterEngine
import io.flutter.plugin.common.MethodChannel

class MainActivity : FlutterFragmentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        if (!BuildConfig.DEBUG) {
            window.setFlags(
                WindowManager.LayoutParams.FLAG_SECURE,
                WindowManager.LayoutParams.FLAG_SECURE,
            )
        }
    }

    override fun configureFlutterEngine(flutterEngine: FlutterEngine) {
        super.configureFlutterEngine(flutterEngine)
        MethodChannel(flutterEngine.dartExecutor.binaryMessenger, "arquitectos/seguridad")
            .setMethodCallHandler { llamada, resultado ->
                when (llamada.method) {
                    "dispositivoSeguro" -> {
                        val keyguard =
                            getSystemService(Context.KEYGUARD_SERVICE) as KeyguardManager
                        resultado.success(keyguard.isDeviceSecure)
                    }
                    "esEmulador" -> resultado.success(esEmulador())
                    else -> resultado.notImplemented()
                }
            }
    }

    private fun esEmulador(): Boolean {
        val huella = Build.FINGERPRINT
        val modelo = Build.MODEL
        val fabricante = Build.MANUFACTURER
        val marca = Build.BRAND
        val dispositivo = Build.DEVICE
        val producto = Build.PRODUCT
        val hardware = Build.HARDWARE
        return huella.startsWith("generic")
            || huella.startsWith("unknown")
            || modelo.contains("google_sdk")
            || modelo.contains("Emulator")
            || modelo.contains("Android SDK built for x86")
            || fabricante.contains("Genymotion")
            || (marca.startsWith("generic") && dispositivo.startsWith("generic"))
            || producto.contains("sdk_gphone")
            || producto.contains("emulator")
            || producto.contains("simulator")
            || hardware.contains("goldfish")
            || hardware.contains("ranchu")
    }
}
