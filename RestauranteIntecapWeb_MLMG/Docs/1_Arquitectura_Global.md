# 🏗️ 1. Arquitectura Global del Sistema

## Pila Tecnológica (Tech Stack)
* **Lenguaje:** C#
* **Framework:** .NET 8 (ASP.NET Core)
* **Patrón de Diseño:** MVC (Modelo-Vista-Controlador)
* **ORM (Mapeo de Base de Datos):** Entity Framework Core
* **Base de Datos:** Microsoft SQL Server
* **Frontend:** HTML5, CSS3, Bootstrap 5, Razor Syntax
* **Librerías Extra:** ClosedXML (Reportes Excel), QuestPDF (Reportes PDF)

---

## El Patrón MVC (Modelo-Vista-Controlador)
Este proyecto utiliza una arquitectura monolítica bajo el patrón MVC, dividiendo las responsabilidades en tres capas principales:

1. **Modelos (Models / DTOs):** Representan la estructura de los datos. Las clases en `Models/Entidades.cs` son el reflejo exacto de las tablas en SQL Server. Los `DTOs` (Data Transfer Objects) son cajas de transporte que llevan información específica entre el servidor y las vistas sin exponer toda la base de datos.
2. **Vistas (Views):** Archivos `.cshtml` que combinan HTML con C# (Razor). Son la interfaz gráfica que ve el usuario.
3. **Controladores (Controllers):** Son los directores de orquesta. Reciben la petición web (clic del usuario), llaman a los Servicios para procesar la lógica, y deciden qué Vista devolver.

---

## Flujo de Vida de una Petición (Request Lifecycle)
Para entender cómo funciona el sistema, este es el recorrido exacto de los datos cuando un usuario interactúa con la aplicación (Ejemplo: *Hacer una reserva*):

1. **Usuario interactúa (Frontend):** El usuario hace clic en "Confirmar Reserva" en la vista HTML.
2. **Controlador recibe (Controller):** `EmpleadoController.cs` intercepta la petición HTTP POST y recibe un DTO con los datos.
3. **Controlador delega (Dependency Injection):** El controlador NO procesa la reserva. Se la pasa al servicio inyectado `_empleadoService.ProcesarReservaAsync()`.
4. **Lógica de Negocio (Service):** `EmpleadoService.cs` aplica las reglas (verifica stock, valida límite de almuerzos del rol y estructura el NIT).
5. **Acceso a Datos (ApplicationDbContext):** El servicio utiliza Entity Framework (`_context.Reservas.Add(...)`) para preparar el guardado.
6. **Base de Datos (SQL Server):** Entity Framework traduce el código C# a comandos SQL nativos (`INSERT INTO...`) y los ejecuta.
7. **Respuesta al Usuario:** El flujo regresa hacia arriba. El Servicio avisa al Controlador que hubo éxito, y el Controlador envía un mensaje `Ok()` o actualiza la Vista para que el usuario vea su reserva confirmada.

---

## Inyección de Dependencias
El archivo `Program.cs` es el corazón de la configuración. Allí registramos nuestros servicios:
`builder.Services.AddScoped<IEmpleadoService, EmpleadoService>();`
Esto significa que el framework se encarga de crear y destruir los servicios automáticamente por cada petición web, manteniendo el sistema optimizado y la memoria limpia.