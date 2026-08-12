# 🛠️ 7. Guías Prácticas de Mantenimiento

## 📝 ¿Cómo modificar el límite de platillos de un Empleado/Rol?
**Problema:** El departamento de RRHH autoriza que el personal de cocina ahora pueda reservar 10 almuerzos diarios.
**Solución:** 
No necesitas tocar el código C#. El sistema lee esto dinámicamente.
1. Entra a SQL Server Management Studio.
2. Haz un `UPDATE roles SET max_almuerzos = 10 WHERE nombre = 'Cocina'`.
3. El sistema aplicará la regla automáticamente en la próxima reserva.

## 📝 ¿Cómo agregar una nueva columna a un reporte PDF?
**Problema:** Te piden agregar la columna "Fecha de Nacimiento" al PDF de Usuarios.
**Solución:**
1. Ve al archivo `Models/DTOs/AdminDTOs.cs` y agrega la propiedad `FechaNacimiento` en `UsuarioAdminDTO`.
2. Ve al `AdminService.cs` y localiza el método `ObtenerTodosLosUsuariosAsync()`. Mapea la nueva columna en el `Select`.
3. En el mismo `AdminService.cs`, ve a `GenerarPdfUsuariosAsync()`. 
4. Agrega una nueva columna relativa en `table.ColumnsDefinition`.
5. Agrega el encabezado en `table.Header`.
6. Agrega la celda de datos en el bucle `foreach (var u in usuarios)`.

## 📝 ¿Cómo agregar una nueva opción al menú superior?
**Problema:** Creaste una nueva pantalla llamada "Soporte" y quieres que el Administrador la vea en la barra.
**Solución:**
1. Abre `Views/Shared/_Layout.cshtml`.
2. Busca el bloque `@if (User.IsInRole("Administrador"))`.
3. Copia un `<li>` existente y cambia el `asp-controller` y `asp-action` hacia tu nueva ruta. Asegúrate de conservar la clase `.nav-btn` para mantener la estética.