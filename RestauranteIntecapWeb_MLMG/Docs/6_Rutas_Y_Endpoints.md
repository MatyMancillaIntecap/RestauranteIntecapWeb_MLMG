# 🌍 6. Rutas y Endpoints (API / MVC)

El sistema utiliza el enrutamiento estándar de ASP.NET Core MVC: `[Dominio]/[Controlador]/[Acción]`

## 🔐 Autenticación
* **GET `/Account/Login`** - Muestra la pantalla de inicio de sesión. (Público)
* **POST `/Account/Login`** - Procesa las credenciales. (Público)
* **GET `/Account/Logout`** - Destruye la Cookie de sesión. (Autenticados)

## 👨‍💼 Administrador (Requiere Rol: Administrador)
* **GET `/Admin/Index`** - Dashboard y métricas financieras.
* **GET `/Admin/Usuarios`** - Lista de usuarios y control de acceso.
* **GET `/Admin/DescargarReporteExcel`** - Descarga del reporte general.
* **GET `/Admin/DescargarReportePdf`** - Descarga del reporte general.

## 🍳 Cocina (Requiere Rol: Cocina o Administrador)
* **GET `/Cocina/Index`** - Panel de control de platillos e inventario del día.
* **POST `/Cocina/PublicarMenu`** - Guarda un nuevo platillo en `menu_diario`.

## 🍽️ Reservas / Empleados (Requiere Rol: Empleado, Cocina o Administrador)
* **GET `/Empleado/Index`** - Catálogo del menú del día disponible.
* **POST `/Empleado/RealizarReserva`** - Endpoint AJAX (JSON) que procesa la compra respetando el límite dinámico de almuerzos del rol.
* **GET `/Empleado/Historial`** - Bitácora de consumo personal filtrable por fechas.