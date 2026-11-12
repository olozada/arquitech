# Operación

## Respaldos de ARQUITECTOS

La base ya se perdió una vez. Hace falta un respaldo completo cada día y un respaldo del registro de transacciones cada 15 minutos, en modo de recuperación FULL, y una copia de esos archivos fuera del servidor. Hay que probar la restauración en otra base antes de darlo por listo.

Quien opera SQL Server ejecuta estos scripts. No los corre la aplicación.

Si `WITH COMPRESSION` falla porque la edición no lo admite, quite esa palabra y deje `CHECKSUM`.

### 1. Carpeta y modo de recuperación

Cambie `D:\Respaldos\ARQUITECTOS` si el disco de datos es otro. La carpeta no debe estar en el mismo disco que los archivos de la base.

La cuenta del servicio de SQL Server necesita permiso de escritura en esa carpeta.

```sql
ALTER DATABASE [ARQUITECTOS] SET RECOVERY FULL;
```

Cree la carpeta en el servidor y haga el primer completo. A partir de ese archivo empieza la cadena de registros.

```sql
BACKUP DATABASE [ARQUITECTOS]
TO DISK = N'D:\Respaldos\ARQUITECTOS\ARQUITECTOS_full_inicial.bak'
WITH COMPRESSION, CHECKSUM, STATS = 10;

RESTORE VERIFYONLY
FROM DISK = N'D:\Respaldos\ARQUITECTOS\ARQUITECTOS_full_inicial.bak'
WITH CHECKSUM;
```

### 2. Completo diario

```sql
DECLARE @carpeta nvarchar(260) = N'D:\Respaldos\ARQUITECTOS\';
DECLARE @archivo nvarchar(400) =
    @carpeta + N'ARQUITECTOS_full_' + CONVERT(char(8), GETDATE(), 112) + N'.bak';

BACKUP DATABASE [ARQUITECTOS]
TO DISK = @archivo
WITH COMPRESSION, CHECKSUM, STATS = 10;

RESTORE VERIFYONLY FROM DISK = @archivo WITH CHECKSUM;
```

### 3. Registro cada 15 minutos

```sql
DECLARE @carpeta nvarchar(260) = N'D:\Respaldos\ARQUITECTOS\';
DECLARE @archivo nvarchar(400) =
    @carpeta + N'ARQUITECTOS_log_' +
    CONVERT(char(8), GETDATE(), 112) + N'_' +
    REPLACE(CONVERT(char(8), GETDATE(), 108), ':', '') + N'.trn';

BACKUP LOG [ARQUITECTOS]
TO DISK = @archivo
WITH COMPRESSION, CHECKSUM;
```

El registro solo es válido si existe el completo que abrió la cadena y no se ha cambiado la base a SIMPLE en medio.

### 4. Trabajos del Agente SQL Server

Ejecute esto una vez, conectado a la instancia con permiso para crear trabajos. El completo corre a las 23:00. El registro corre todo el día, cada 15 minutos.

```sql
USE msdb;

IF NOT EXISTS (SELECT 1 FROM dbo.sysjobs WHERE name = N'ARQUITECTOS completo diario')
BEGIN
    EXEC dbo.sp_add_job
        @job_name = N'ARQUITECTOS completo diario',
        @enabled = 1;

    EXEC dbo.sp_add_jobstep
        @job_name = N'ARQUITECTOS completo diario',
        @step_name = N'Respaldo completo',
        @subsystem = N'TSQL',
        @command = N'
DECLARE @carpeta nvarchar(260) = N''D:\Respaldos\ARQUITECTOS\'';
DECLARE @archivo nvarchar(400) =
    @carpeta + N''ARQUITECTOS_full_'' + CONVERT(char(8), GETDATE(), 112) + N''.bak'';
BACKUP DATABASE [ARQUITECTOS]
TO DISK = @archivo
WITH COMPRESSION, CHECKSUM, STATS = 10;
RESTORE VERIFYONLY FROM DISK = @archivo WITH CHECKSUM;',
        @database_name = N'master';

    EXEC dbo.sp_add_schedule
        @schedule_name = N'ARQUITECTOS diario 23:00',
        @freq_type = 4,
        @freq_interval = 1,
        @active_start_time = 230000;

    EXEC dbo.sp_attach_schedule
        @job_name = N'ARQUITECTOS completo diario',
        @schedule_name = N'ARQUITECTOS diario 23:00';

    EXEC dbo.sp_add_jobserver
        @job_name = N'ARQUITECTOS completo diario';
END;

IF NOT EXISTS (SELECT 1 FROM dbo.sysjobs WHERE name = N'ARQUITECTOS registro 15 min')
BEGIN
    EXEC dbo.sp_add_job
        @job_name = N'ARQUITECTOS registro 15 min',
        @enabled = 1;

    EXEC dbo.sp_add_jobstep
        @job_name = N'ARQUITECTOS registro 15 min',
        @step_name = N'Respaldo del registro',
        @subsystem = N'TSQL',
        @command = N'
DECLARE @carpeta nvarchar(260) = N''D:\Respaldos\ARQUITECTOS\'';
DECLARE @archivo nvarchar(400) =
    @carpeta + N''ARQUITECTOS_log_'' +
    CONVERT(char(8), GETDATE(), 112) + N''_'' +
    REPLACE(CONVERT(char(8), GETDATE(), 108), '':'', '''') + N''.trn'';
BACKUP LOG [ARQUITECTOS]
TO DISK = @archivo
WITH COMPRESSION, CHECKSUM;',
        @database_name = N'master';

    EXEC dbo.sp_add_schedule
        @schedule_name = N'ARQUITECTOS cada 15 minutos',
        @freq_type = 4,
        @freq_interval = 1,
        @freq_subday_type = 4,
        @freq_subday_interval = 15,
        @active_start_time = 0;

    EXEC dbo.sp_attach_schedule
        @job_name = N'ARQUITECTOS registro 15 min',
        @schedule_name = N'ARQUITECTOS cada 15 minutos';

    EXEC dbo.sp_add_jobserver
        @job_name = N'ARQUITECTOS registro 15 min';
END;
```

Revise el historial del Agente al día siguiente. Un error de permiso o de disco tiene que verse ahí, no solo en la aplicación.

### 5. Copia fuera del servidor

Un respaldo que se queda en el mismo equipo se pierde con el equipo. Copie la carpeta a otro servidor o a un disco que no esté en esta máquina. Ejemplo, en el servidor de SQL, después de cada respaldo:

```text
robocopy D:\Respaldos\ARQUITECTOS \\servidor-copias\ARQUITECTOS /E /Z /XO /R:2 /W:5
```

Cambie `\\servidor-copias\ARQUITECTOS` por la ruta real. Programe esa copia con una frecuencia que alcance a llevarse también los registros de 15 minutos.

Conserve fuera del servidor al menos 14 días de completos y todos los registros desde el completo más antiguo que todavía quiera poder restaurar.

### 6. Probar la restauración en otra base

No restaure encima de `ARQUITECTOS`. El ensayo crea `ARQUITECTOS_RESTAURACION` y al final se borra.

Anote los nombres lógicos actuales:

```sql
SELECT name, physical_name, type_desc
FROM sys.master_files
WHERE database_id = DB_ID(N'ARQUITECTOS');
```

En el archivo de respaldo pueden confirmarse así. Use el completo del día y, en orden de hora, los `.trn` posteriores.

```sql
RESTORE FILELISTONLY
FROM DISK = N'D:\Respaldos\ARQUITECTOS\ARQUITECTOS_full_AAAAMMDD.bak';
```

1. Restaure el completo sin poner la base en línea. Sustituya los nombres lógicos y las rutas de `MOVE` por los del paso anterior, apuntando a archivos nuevos.

```sql
RESTORE DATABASE [ARQUITECTOS_RESTAURACION]
FROM DISK = N'D:\Respaldos\ARQUITECTOS\ARQUITECTOS_full_AAAAMMDD.bak'
WITH MOVE N'ARQUITECTOS' TO N'D:\Datos\ARQUITECTOS_RESTAURACION.mdf',
     MOVE N'ARQUITECTOS_log' TO N'D:\Datos\ARQUITECTOS_RESTAURACION_log.ldf',
     NORECOVERY, CHECKSUM, STATS = 10;
```

2. Aplique cada registro, menos el último, con `NORECOVERY`.

```sql
RESTORE LOG [ARQUITECTOS_RESTAURACION]
FROM DISK = N'D:\Respaldos\ARQUITECTOS\ARQUITECTOS_log_AAAAMMDD_HHMMSS.trn'
WITH NORECOVERY, CHECKSUM;
```

3. El último registro deja la base lista.

```sql
RESTORE LOG [ARQUITECTOS_RESTAURACION]
FROM DISK = N'D:\Respaldos\ARQUITECTOS\ARQUITECTOS_log_AAAAMMDD_HHMMSS.trn'
WITH RECOVERY, CHECKSUM;
```

Si solo quiere comprobar el completo, use `WITH RECOVERY` en el `RESTORE DATABASE` y no aplique registros.

4. Compruebe que responde y que tiene datos. Por ejemplo:

```sql
SELECT COUNT(*) AS usuarios FROM ARQUITECTOS_RESTAURACION.dbo.Usuarios;
SELECT COUNT(*) AS actas FROM ARQUITECTOS_RESTAURACION.dbo.Actas;
```

5. Borre la base de ensayo cuando el conteo sea el esperado.

```sql
ALTER DATABASE [ARQUITECTOS_RESTAURACION] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
DROP DATABASE [ARQUITECTOS_RESTAURACION];
```

### Pendiente de quien opera el servidor

- Dejar `ARQUITECTOS` en recuperación FULL y crear los dos trabajos del Agente.
- Copiar `D:\Respaldos\ARQUITECTOS` fuera del servidor.
- Restaurar en `ARQUITECTOS_RESTAURACION`, revisar los conteos y borrar esa base.

## Iconos del menú

La migración `IconosLucide` cambia `MenuOpciones.Icono`: deja de guardar la clase de Font Awesome y guarda el nombre del ícono de Lucide. Aplique `backend/scripts/actualizar_local.sql` junto con la publicación del frontend nuevo. El frontend nuevo todavía dibuja el ícono si la columna conserva la clase anterior. El frontend publicado no entiende los nombres de Lucide, así que no aplique esa parte del script antes de copiar el frontend.
