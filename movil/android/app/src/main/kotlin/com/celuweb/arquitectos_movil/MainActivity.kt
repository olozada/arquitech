package com.celuweb.arquitectos_movil

import android.app.KeyguardManager
import android.content.Context
import android.os.Bundle
import android.view.WindowManager
import io.flutter.embedding.android.FlutterActivity
import io.flutter.embedding.engine.FlutterEngine
import io.flutter.plugin.common.MethodChannel

class MainActivity : FlutterActivity() {
    override fun configureFlutterEngine(flutterEngine: FlutterEngine) {
        super.configureFlutterEngine(flutterEngine)
        MethodChannel(
            flutterEngine.dartExecutor.binaryMessenger,
            "arquitectos/seguridad",
        ).setMethodCallHandler { llamada, resultado ->
            when (llamada.method) {
                "dispositivoSeguro" -> {
                    val km =
                        getSystemService(Context.KEYGUARD_SERVICE) as KeyguardManager
                    resultado.success(km.isDeviceSecure)
                }
                else -> resultado.notImplemented()
            }
        }
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        if (!BuildConfig.DEBUG) {
            window.setFlags(
                WindowManager.LayoutParams.FLAG_SECURE,
                WindowManager.LayoutParams.FLAG_SECURE,
            )
        }
    }
}
