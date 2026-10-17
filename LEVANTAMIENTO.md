# iRoute Commerce: levantamiento del entorno

Solución del reto técnico iRoute: carga del archivo `commerce_DDMMYYYY.csv`, procesamiento por fecha (`pc_processdate`) y consulta de los registros en cuarentena.

| Capa | Tecnología | Carpeta |
|------|------------|---------|
| Base de datos | SQL Server (scripts T-SQL + stored procedures) | [`Database/`](Database/) |
| Backend | .NET 9 Web API (Dapper + Microsoft.Data.SqlClient, JWT, Swagger) | [`Backend/`](Backend/) |
| Frontend | Angular 20 (standalone, signals, zoneless, lazy loading) | [`Frontend/`](Frontend/) |
| Datos de prueba | CSV de ejemplo | [`samples/`](samples/) |

---

## 1. Requisitos previos

| Herramienta | Versión usada | Verificar |
|-------------|---------------|-----------|
| SQL Server (Express/Developer/LocalDB) | 2019 o superior | `sqlcmd -S localhost -E -Q "SELECT @@VERSION"` |
| `sqlcmd` o SSMS | cualquiera | `sqlcmd -?` |
| .NET SDK | 9.0.x | `dotnet --list-sdks` |
| Node.js | 20.19+ / 22.12+ | `node -v` |
| npm | 10+ | `npm -v` |

> Angular CLI no hace falta instalarlo de forma global: se usa el del proyecto con `npx ng` o `npm start`.

---

## 2. Base de datos

Los scripts están numerados en el orden de ejecución:

```
Database/
├── deploy.sql                               # ejecuta todos los scripts en orden (modo SQLCMD)
└── scripts/
    ├── 01_create_database.sql               # BD IrouteCommerce
    ├── 02_create_tables.sql                 # commerce y commerce_quarantine
    ├── 03_alter_commerce_quarantine.sql     # ALTER TABLE: agrega la columna "motivo"
    ├── 04_create_types.sql                  # TVP dbo.commerce_type (carga masiva)
    ├── 05_fn_commerce_motivo.sql            # reglas de validación compartidas
    ├── 06_sp_create_commerce.sql            # registra las filas del CSV (rechaza duplicados)
    ├── 07_sp_validate_commerce.sql          # valida y mueve a cuarentena
    ├── 08_sp_list_commerce_quarantine.sql   # lista la cuarentena
    ├── 09_sp_list_commerce_dates.sql        # fechas cargadas (pantalla Procesar)
    ├── 10_sp_update_commerce_quarantine.sql # corrige y revalida un registro en cuarentena
    └── 11_sp_delete_commerce_quarantine.sql # descarta un registro en cuarentena
```

> Si ya tenías la BD de una versión anterior, vuelve a ejecutar `deploy.sql`: agrega lo nuevo (función, índices y SP) sin borrar datos.

**Opción A: sqlcmd (recomendada)**

```bash
cd Database
sqlcmd -S localhost -E -f 65001 -i deploy.sql
```

- `-E` usa autenticación de Windows. Con usuario SQL: `-U sa -P <password>`.
- `-f 65001` lee los scripts en UTF-8 (respeta tildes y la “ñ” de los motivos).
- Para una instancia con nombre: `-S localhost\SQLEXPRESS`; para LocalDB: `-S "(localdb)\MSSQLLocalDB"`.

**Opción B: SSMS / Azure Data Studio**

Abre y ejecuta los scripts de `Database/scripts/` uno por uno, del `01` al `08`.

Los scripts son idempotentes, así que puedes ejecutarlos de nuevo sin errores.

---

## 3. Backend (.NET 9)

### 3.1 Configuración

Archivo `Backend/src/Iroute.Commerce.Api/appsettings.json`:

```json
"ConnectionStrings": {
  "CommerceDb": "Server=localhost;Database=IrouteCommerce;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

Ajusta `Server=` a tu instancia. Con usuario SQL, reemplaza `Trusted_Connection=True` por `User Id=sa;Password=<password>`.

Otras secciones:

| Sección | Para qué sirve |
|---------|----------------|
| `Jwt` | Emisor, audiencia, clave y expiración del token. **Cambia `Key` fuera de un entorno local.** |
| `DemoUser` | Credenciales del login de demostración (`admin` / `Admin123*`). |
| `Cors:AllowedOrigins` | Origen del frontend (`http://localhost:4200`). |

### 3.2 Ejecutar

```bash
cd Backend
dotnet restore
dotnet build
dotnet run --project src/Iroute.Commerce.Api --launch-profile http
```

- API: `http://localhost:5102`
- Swagger: http://localhost:5102/swagger

Para probar desde Swagger: ejecuta `POST /api/auth/login`, copia el `token`, pulsa **Authorize** y pégalo.

### 3.3 Pruebas unitarias

```bash
cd Backend
dotnet test
```

### 3.4 Endpoints

Todos los endpoints de `/api/commerce` requieren `Authorization: Bearer <token>`.

| Método | Ruta | Descripción | Stored procedure |
|--------|------|-------------|------------------|
| POST | `/api/auth/login` | Devuelve el JWT. | — |
| POST | `/api/commerce/upload` | `multipart/form-data` con el campo `file` (`commerce_DDMMYYYY.csv`). Responde **400** si el archivo está vacío, el nombre no cumple el formato o el contenido es inválido, y **409** si la fecha ya fue cargada o algún comercio ya existe (ver sección 6). | `sp_create_commerce` |
| POST | `/api/commerce/process` | Body `{ "processDate": "2026-10-16" }`. Devuelve la cantidad de registros enviados a `commerce_quarantine`. | `sp_validate_commerce` |
| GET | `/api/commerce/quarantine?processDate=` | Lista los comercios en cuarentena (la fecha es opcional). | `sp_list_commerce_quarantine` |
| PUT | `/api/commerce/quarantine/{id}` | Corrige un registro y lo revalida. Responde `{ "released": true }` si volvió a `commerce`, o `{ "released": false, "motivo": "…" }` si sigue en cuarentena. **404** si no existe. | `sp_update_commerce_quarantine` |
| DELETE | `/api/commerce/quarantine/{id}` | Descarta un registro en cuarentena (**204**). **404** si no existe. | `sp_delete_commerce_quarantine` |
| GET | `/api/commerce/process-dates` | Fechas cargadas con su total de registros. | `sp_list_commerce_dates` |

Ejemplo con curl:

```bash
TOKEN=$(curl -s -X POST http://localhost:5102/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"Admin123*"}' | sed -E 's/.*"token":"([^"]+)".*/\1/')

curl -H "Authorization: Bearer $TOKEN" -F "file=@samples/commerce_16102026.csv" http://localhost:5102/api/commerce/upload
curl -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"processDate":"2026-10-16"}' http://localhost:5102/api/commerce/process
curl -H "Authorization: Bearer $TOKEN" "http://localhost:5102/api/commerce/quarantine?processDate=2026-10-16"
```

---

## 4. Frontend (Angular 20)

### 4.1 Configuración

La URL del API está en `Frontend/src/environments/environment.development.ts` (desarrollo) y en `environment.ts` (producción):

```ts
apiUrl: 'http://localhost:5102/api'
```

### 4.2 Ejecutar

```bash
cd Frontend
npm install
npm start          # equivale a: npx ng serve
```

Abre http://localhost:4200 e ingresa con **admin / Admin123\***.

Build de producción: `npm run build`. El resultado queda en `Frontend/dist/Frontend`.

### 4.3 Pantallas

1. **Login** (opcional en el reto): autenticación con JWT.
2. **Cargar archivo**: selección o arrastre del CSV, validación del nombre y la cabecera, y previsualización paginada antes de enviar. Marca las filas que probablemente irán a cuarentena y bloquea el envío si alguna fecha no coincide con la del nombre o si hay códigos de comercio vacíos o repetidos.
3. **Procesar**: muestra las fechas cargadas, permite elegir `pc_processdate`, ejecuta la validación e indica cuántos registros fueron a cuarentena.
4. **Cuarentena**: listado con filtro por fecha, búsqueda y motivos del rechazo. Cada registro se puede **corregir** (un diálogo indica en vivo si quedará válido) o **descartar**.

---

## 5. Orden de arranque y prueba rápida

1. Ejecuta `Database/deploy.sql`.
2. Inicia el backend en `http://localhost:5102`.
3. Inicia el frontend en `http://localhost:4200`.
4. Inicia sesión, carga `samples/commerce_16102026.csv`, procesa la fecha **16/10/2026** y revisa la cuarentena.

Resultado esperado con el CSV de ejemplo: **10** registros cargados, **6** en cuarentena y **4** válidos que permanecen en `commerce`.

Para reiniciar los datos de prueba:

```sql
USE IrouteCommerce;
TRUNCATE TABLE dbo.commerce;
TRUNCATE TABLE dbo.commerce_quarantine;
```

---

## 6. Estructura del CSV

El enunciado no incluye un archivo de muestra, así que se definió esta estructura. Las columnas `pc_processdate`, `pc_nomcomred` y `pc_numdoc` son las que exige el enunciado:

```
pc_processdate,pc_codcomred,pc_nomcomred,pc_tipdoc,pc_numdoc,pc_direccion,pc_ciudad,pc_telefono,pc_email
```

- Delimitador: `,` o `;` (se detecta automáticamente).
- Admite campos entre comillas (`"Restaurante El Sabor, Cía. Ltda."`).
- Formatos de `pc_processdate`: `yyyy-MM-dd`, `dd/MM/yyyy`, `ddMMyyyy` o `yyyyMMdd`.
- Codificación: UTF-8.

### Reglas contra cargas duplicadas

`pc_codcomred` identifica al comercio y es obligatorio. Una carga se acepta completa o se rechaza completa:

| Regla | Dónde se valida | Respuesta |
|-------|-----------------|-----------|
| Todas las filas tienen `pc_processdate` igual a la fecha del nombre del archivo. Así, renombrar el archivo no permite volver a subirlo. | Previsualización + API | 400 |
| `pc_codcomred` es obligatorio y no se repite dentro del archivo. | Previsualización + API + SP | 400 |
| La fecha de proceso no tiene una carga previa (en `commerce` ni en cuarentena). | SP | 409 |
| Ningún comercio del archivo existe ya en `commerce` ni en `commerce_quarantine`, aunque venga en un archivo de otra fecha. | SP | 409 |

El SP serializa las cargas con `sp_getapplock`, así que dos cargas simultáneas no pueden pasar estas validaciones al mismo tiempo.

### Ciclo de vida de un registro en cuarentena

1. Al procesar una fecha, los registros inválidos pasan de `commerce` a `commerce_quarantine` con su motivo.
2. Desde la pantalla Cuarentena se corrigen los datos (`pc_processdate` y `pc_codcomred` no se editan). El SP los revalida con las mismas reglas (`fn_commerce_motivo`):
   - si ahora son válidos, el registro **vuelve a `commerce`** y sale de la cuarentena;
   - si no, se guardan los cambios y se actualiza el motivo.
3. Si un registro no se puede corregir, se puede **descartar**.

Si el archivo real usa otras columnas, solo hay que ajustar estos puntos: la tabla y el TVP en `Database/scripts`, `CommerceCsvParser` y `CommerceRepository` en el backend, y `commerce.models.ts` en el frontend.

---

## 7. Arquitectura y decisiones técnicas

```
Backend/
├── src/
│   ├── Iroute.Commerce.Api             # Controladores, JWT, Swagger, manejo global de errores (ProblemDetails)
│   ├── Iroute.Commerce.Application     # Casos de uso, parser CSV, DTOs, interfaces (sin dependencias de infraestructura)
│   └── Iroute.Commerce.Infrastructure  # Repositorio Dapper que invoca los stored procedures
└── tests/
    └── Iroute.Commerce.Tests           # xUnit: parser CSV y validación del nombre del archivo

Frontend/src/app/
├── core/       # modelos, servicios HTTP, interceptores (token y errores), guards
├── layout/     # shell con la navegación
├── shared/     # parser CSV, reglas de previsualización, toasts
└── features/   # login, upload, process, quarantine (cada una con carga diferida)
```

**Base de datos**

- `sp_create_commerce` recibe todas las filas en un único **Table-Valued Parameter**: una sola llamada a la BD en lugar de una por fila.
- Las reglas de validación viven en una sola función inline (`fn_commerce_motivo`) que usan tanto el proceso como la corrección de la cuarentena. Un registro puede tener varios motivos, separados por `; `.
- `sp_validate_commerce` hace `DELETE … OUTPUT … INTO commerce_quarantine` dentro de una transacción, de modo que eliminar e insertar ocurre en una sola sentencia atómica.
- Los errores de negocio de los SP (`THROW 5000x`) se traducen en el backend a 400, 404 o 409 con el mensaje del SP.
- La detección de letras y caracteres especiales usa una collation binaria (`Latin1_General_100_BIN2`), para que los rangos `[0-9]` y `[A-Za-z]` no dependan de reglas lingüísticas. También considera tildes y la “ñ”.
- Hay índices por `pc_processdate` en ambas tablas.

**Backend**

- Arquitectura por capas con inyección de dependencias. Usa Dapper porque todo el acceso es mediante stored procedures.
- Errores centralizados con `IExceptionHandler`: devuelve **400** para errores de validación y **500** sin exponer detalles internos.
- El CSV se lee en streaming y tiene un límite de 10 MB. Se informa la línea exacta de cada error de formato.

**Frontend**

- Componentes standalone con `OnPush`, signals y `linkedSignal`, detección de cambios sin zone.js, rutas con carga diferida y bloques de control (`@if` / `@for`).
- Interceptores funcionales: uno agrega el token y otro gestiona los errores (un 401 cierra la sesión).
- La previsualización replica las reglas del SP solo como referencia. La validación oficial la hace la base de datos.

---

## 8. Problemas comunes

| Síntoma | Solución |
|---------|----------|
| `A network-related or instance-specific error` al iniciar el backend | Revisa `Server=` en la cadena de conexión y que el servicio de SQL Server esté en ejecución. |
| `Login failed for user` | Usa un usuario SQL o da permisos a tu usuario de Windows sobre la BD `IrouteCommerce`. |
| Los motivos aparecen sin tildes (`vac?o`) | Vuelve a ejecutar los scripts con `sqlcmd -f 65001` o ábrelos en SSMS como UTF-8. |
| Error CORS en el navegador | El origen del frontend debe estar en `Cors:AllowedOrigins`. |
| “No se pudo conectar con el servidor” en el frontend | El backend no está en ejecución o `apiUrl` no coincide con su puerto. |
| `ng` pide una versión más nueva de Node | Usa Node 20.19+ o 22.12+. |
