# ⚡ 8. Índice Rápido: ¿Dónde modifico qué?

Si necesitas hacer un cambio urgente y no recuerdas dónde está, busca tu requerimiento en esta tabla:

| ¿Qué necesitas modificar? | 📂 Archivo / Capa donde debes buscar |
| :--- | :--- |
| **Límites de almuerzos (Regla de cantidad)** | Base de datos (Tabla `roles` -> columna `max_almuerzos`). La lógica está en `EmpleadoService.cs`. |
| **Colores, logo o menú superior** | `Views/Shared/_Layout.cshtml` |
| **Fondo de imagen de la pantalla de Login** | `Views/Account/Login.cshtml` |
| **Validaciones del NIT o DPI** | `EmpleadoService.cs` (Método `ProcesarReservaAsync`). |
| **Diseño y columnas del Reporte Excel** | `AdminService.cs` (Método `GenerarReporteGlobalExcelAsync`). Busca el objeto `XLWorkbook`. |
| **Diseño y columnas del Reporte PDF** | `AdminService.cs` (Método `GenerarReporteGlobalPdfAsync`). Busca el objeto `Document.Create`. |
| **Ocultar el botón "Cancelar Reserva"** | `Views/Empleado/Historial.cshtml` (Como lo hicimos, removiendo las columnas de Estado y Acciones). |
| **Pantallas amigables de Error 404 / 500** | `Views/Account/Error404.cshtml` y `Controllers/AccountController.cs` |
| **Conexión a la Base de Datos (Cadena SQL)** | Archivo raíz `appsettings.json` (Llave `ConexionSQL`). |
| **Tiempo de expiración de sesión** | `Program.cs` (Propiedad `options.ExpireTimeSpan`). |