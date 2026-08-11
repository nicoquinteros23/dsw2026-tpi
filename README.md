# Trabajo Práctico Integrador
## Desarrollo de Software 2026

# INTEGRANTES:
* Messina Florencia, legajo: 53324
* Bugeau Valentina, legajo: 53133
* Quinteros Nicolas, legajo: 53049

---

## Cómo ejecutar el proyecto localmente

### Requisitos Previos
* **.NET 8 SDK** instalado en tu sistema.
* **SQL Server LocalDB** (instalado por defecto con Visual Studio) o una instancia de SQL Server.

### Pasos para Configurar y Ejecutar

1. **Restaurar dependencias NuGet**:
   Abre una terminal en la raíz del proyecto y ejecuta:
   ```bash
   dotnet restore
   ```

2. **Configuración de la base de datos (Opcional)**:
   Por defecto, el proyecto está configurado para utilizar SQL Server LocalDB. Si necesitas cambiar la cadena de conexión, edita la propiedad `ConnectionStrings.DefaultConnection` en el archivo [appsettings.Development.json](file:///c:/Users/albar/OneDrive/Documentos/TFI/dsw2026-tpi/Dsw2026Tpi.Api/appsettings.Development.json):
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=Dsw2026TpiDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
   }
   ```

3. **Ejecutar el proyecto**:
   Para iniciar la API, ejecuta el siguiente comando desde la raíz del repositorio:
   ```bash
   dotnet run --project Dsw2026Tpi.Api
   ```
   O bien, abre la solución (`Dsw2026Tpi.sln`) en Visual Studio o VS Code y presiona **F5** seleccionando el perfil `Dsw2026Tpi.Api`.

4. **Migraciones Automáticas y Datos Iniciales**:
   Al iniciar la aplicación, las migraciones se aplican automáticamente en la base de datos local (mediante `Database.MigrateAsync()`). También se realiza una siembra inicial de roles e incluye un **usuario administrador por defecto**:
   * **Usuario**: `admin@dsw2026.com`
   * **Contraseña**: `Admin123`

5. **Acceso a Swagger**:
   Una vez que el servidor esté corriendo, puedes acceder a la interfaz interactiva de Swagger en las siguientes direcciones:
   * **HTTP**: [http://localhost:5278/swagger](http://localhost:5278/swagger)
   * **HTTPS**: [https://localhost:7075/swagger](https://localhost:7075/swagger)

---

## Endpoints Implementados

Todos los endpoints (excepto los de Login y Registro, y algunos endpoints GET específicos indicados a continuación) requieren el envío del Token JWT en el encabezado `Authorization` en formato `Bearer {token}`.

### 1. Autenticación (`/api/auth`)
| Método | Ruta | Descripción | Rol Requerido | Rate Limiting |
| :--- | :--- | :--- | :--- | :--- |
| **POST** | `/api/auth/register-admin` | Registra un nuevo usuario Administrador | Anónimo | Global (100 req/min) |
| **POST** | `/api/auth/login-admin` | Inicia sesión como Administrador y retorna token JWT | Anónimo | `AdminLoginPolicy` (5 req/min) |
| **POST** | `/api/auth/register-patient` | Registra un nuevo Paciente | Anónimo | Global (100 req/min) |
| **POST** | `/api/auth/login-patient` | Inicia sesión como Paciente y retorna token JWT | Anónimo | `PatientLoginPolicy` (10 req/min) |

### 2. Especialidades (`/api/specialties`)
| Método | Ruta | Descripción | Rol Requerido | Rate Limiting |
| :--- | :--- | :--- | :--- | :--- |
| **GET** | `/api/specialties` | Obtiene lista paginada y filtrable por `name` de especialidades activas | Anónimo | Global (100 req/min) |
| **POST** | `/api/specialties` | Registra una nueva especialidad médica | `ADMINISTRADOR` | Global (100 req/min) |
| **PUT** | `/api/specialties/{id}` | Actualiza el nombre/descripción de una especialidad (valida existencia) | `ADMINISTRADOR` | Global (100 req/min) |
| **DELETE** | `/api/specialties/{id}` | Da de baja una especialidad (soft-delete) | `ADMINISTRADOR` | Global (100 req/min) |

### 3. Médicos (`/api/doctors`)
| Método | Ruta | Descripción | Rol Requerido | Rate Limiting |
| :--- | :--- | :--- | :--- | :--- |
| **GET** | `/api/doctors` | Lista todos los médicos activos con paginación | Anónimo | Global (100 req/min) |
| **GET** | `/api/doctors/{id}` | Obtiene el detalle de un médico por su ID | Anónimo | Global (100 req/min) |
| **POST** | `/api/doctors` | Registra un nuevo médico y valida que la especialidad exista | `ADMINISTRADOR` | Global (100 req/min) |
| **PUT** | `/api/doctors/{id}` | Actualiza datos de un médico (valida especialidad) | `ADMINISTRADOR` | Global (100 req/min) |
| **DELETE** | `/api/doctors/{id}` | Da de baja lógica a un médico | `ADMINISTRADOR` | Global (100 req/min) |
| **GET** | `/api/doctors/{id}/availabilities` | Obtiene el listado de turnos/horarios libres (`AVAILABLE`) de un médico | Anónimo | Global (100 req/min) |

### 4. Disponibilidades y Reglas (`/api/availability`)
| Método | Ruta | Descripción | Rol Requerido | Rate Limiting |
| :--- | :--- | :--- | :--- | :--- |
| **POST** | `/api/availability` | Define reglas de atención semanal y genera slots horarios (omitiendo feriados de `holidays.json`) | `ADMINISTRADOR` | Global (100 req/min) |
| **PUT** | `/api/availability/{id}` | Actualiza reglas de atención y regenera slots correspondientes | `ADMINISTRADOR` | Global (100 req/min) |

### 5. Turnos (`/api/appointments`)
| Método | Ruta | Descripción | Rol Requerido | Rate Limiting |
| :--- | :--- | :--- | :--- | :--- |
| **POST** | `/api/appointments` | Reserva un turno médico (valida horarios futuros, datos y cambia slot a `BOOKED`) | `Paciente` | `AppointmentBookingPolicy` (5 req/min por usuario) |
| **GET** | `/api/appointments/patient` | Obtiene los turnos reservados de un paciente mediante query param `dni` | Autenticado | Global (100 req/min) |
| **GET** | `/api/appointments` | Obtiene la lista plana de turnos para un día específico (`?date=YYYY-MM-DD`) | `ADMINISTRADOR` | Global (100 req/min) |
| **GET** | `/api/appointments/search` | Búsqueda avanzada de turnos (especialidad, médico, DNI, fecha) paginado | `ADMINISTRADOR` | Global (100 req/min) |
| **DELETE** | `/api/appointments/{id}` | Cancela un turno activo (vuelve el slot a `AVAILABLE`) | Autenticado | Global (100 req/min) |
