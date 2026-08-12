# 📂 4. Diccionario de Archivos Principales

Este documento es un inventario de los archivos más críticos del sistema y su responsabilidad. Si no sabes dónde buscar una lógica específica, revisa esta lista.

## ⚙️ Configuración y Arranque
* **`Program.cs`**
  * **Qué hace:** Es el punto de entrada de la aplicación. Configura la conexión a la base de datos, la autenticación por Cookies y registra la Inyección de Dependencias (Interfaces vs Servicios).
  * **Cuándo modificarlo:** Si vas a agregar un nuevo Servicio (`IService`), cambiar el tiempo de expiración de la sesión o agregar una nueva librería global.

## 🗄️ Capa de Datos (Data)
* **`ApplicationDbContext.cs`**
  * **Qué hace:** Es el puente entre C# y SQL Server. Contiene los `DbSet` que representan cada tabla de la base de datos.
  * **Cuándo modificarlo:** Si agregas una nueva tabla a la base de datos, debes registrarla aquí.

## 🧠 Capa de Servicios (Lógica de Negocio)
* **`AdminService.cs`**
  * **Qué hace:** Contiene la lógica pesada del administrador: cálculo de métricas del Dashboard, creación de reportes Excel (`ClosedXML`) y PDF (`QuestPDF`), y gestión de usuarios.
* **`EmpleadoService.cs`**
  * **Qué hace:** Evalúa los límites dinámicos de almuerzos según el rol, descuenta stock de los platillos e inserta las reservas en la base de datos.
* **`CocinaService.cs`**
  * **Qué hace:** Gestiona la publicación de menús, controla el inventario de la cocina y genera los consolidados diarios.
* **`AuthService.cs`**
  * **Qué hace:** Valida correos y contraseñas contra SQL Server y registra los accesos en la tabla `historial_login`.

## 🎮 Controladores (Controllers)
* **`AccountController.cs`**: Maneja el inicio de sesión, el cierre (logout) y las redirecciones de error 404/500.
* **`AdminController.cs`**, **`EmpleadoController.cs`**, **`CocinaController.cs`**: Interceptan los clics del usuario, llaman a su respectivo Servicio y devuelven la Vista correspondiente.

## 📦 Modelos y DTOs (Data Transfer Objects)
* **`Models/Entidades.cs`**: Contiene las clases puras que mapean la base de datos (`Usuario`, `Reserva`, `MenuDiario`).
* **`Models/DTOs/`**: Contiene objetos aligerados (ej. `DashboardDTO`, `HistorialEmpleadoDTO`) que viajan hacia las Vistas.

## 🖥️ Vistas Maestras (Frontend)
* **`Views/Shared/_Layout.cshtml`**
  * **Qué hace:** Es el "esqueleto" visual de toda la aplicación. Contiene la barra de navegación superior (Navbar), los estilos en cápsula (`.nav-btn`) y el pie de página.
  * **Cuándo modificarlo:** Para cambiar colores globales, agregar opciones al menú o cambiar el logo de Intecap.