# SQLCipher actual (net.zetetic:sqlcipher-android, usado por sqflite_sqlcipher).
-keep class net.zetetic.** { *; }
-dontwarn net.zetetic.**

# API antigua net.sqlcipher (referencias heredadas / JNI).
-keep class net.sqlcipher.** { *; }

-keep class com.it_nomads.fluttersecurestorage.** { *; }

# sqflite_sqlcipher plugin (com.davidmartos96.sqflite_sqlcipher en pub.dev 3.4.x).
-keep class com.davidmartos96.sqflite_sqlcipher.** { *; }

# sqflite estándar cuando entra por dependencia transitiva.
-keep class com.tekartik.sqflite.** { *; }
