# 🕸️ 5. Mapa de Dependencias del Sistema

El sistema utiliza **Inyección de Dependencias**. Esto significa que las capas superiores dependen de interfaces, no de implementaciones directas.

## 🔄 Flujo de Dependencia Principal

`Vistas (HTML)` ➔ `Controlador (C#)` ➔ `Servicio (Lógica)` ➔ `DbContext (SQL)`

### 1. Dependencias del Controlador de Empleados
Si modificas **`IEmpleadoService`**:
* ⚠️ **Se rompe:** `EmpleadoService.cs` (Debe implementar la nueva firma).
* ⚠️ **Se rompe:** `EmpleadoController.cs` (Si utilizaba el método antiguo).

### 2. Dependencias de Reportes (AdminService)
El `AdminService.cs` es el servicio con más dependencias externas.
* **Depende de:** `ApplicationDbContext` (Para leer datos).
* **Depende de:** `ClosedXML` (Para exportar Excel).
* **Depende de:** `QuestPDF` (Para exportar PDF).
* ⚠️ **Precaución:** Si cambias la estructura de la clase `Reserva`, asegúrate de actualizar el mapeo en las celdas de Excel y PDF dentro de este servicio.

### 3. Dependencias de Autenticación
* `AccountController` depende de `IAuthService`.
* `IAuthService` depende de la tabla `usuarios` y `historial_login`.
* ⚠️ **Precaución:** Si borras o cambias la columna `activo` en la base de datos, el Login fallará.