# 🔐 3. Roles, Permisos y Seguridad

El sistema Restaurante Intecap implementa seguridad mediante **Cookies de Sesión Encripatadas** y utiliza el modelo **RBAC (Role-Based Access Control)** para restringir el acceso a las diferentes pantallas y funciones.

## 👥 Tipos de Usuarios (Roles)

Actualmente, el sistema soporta 3 roles principales, configurados en la tabla `roles`:

### 1. Administrador
* **Nivel de Acceso:** Total (Superusuario).
* **Responsabilidades:**
  * Acceso al Dashboard con métricas y KPI financieros.
  * Gestión de usuarios (Crear, editar, activar/desactivar).
  * Generación de reportes globales en Excel y PDF.
  * Acceso al área de cocina y realización de reservas propias.
* **Límite de almuerzos:** Típicamente configurado como `0` (Ilimitado).

### 2. Cocina
* **Nivel de Acceso:** Operativo.
* **Responsabilidades:**
  * Publicación del Menú del Día.
  * Gestión de stock de platillos.
  * Visualización del consolidado diario de reservas.
  * Capacidad de realizar reservas propias respetando su límite (Ej: 5 platillos).
* **Restricciones:** No puede ver el Dashboard financiero ni administrar usuarios.

### 3. Empleado
* **Nivel de Acceso:** Usuario Final (Consumidor).
* **Responsabilidades:**
  * Visualizar el menú disponible del día.
  * Realizar pedidos seleccionando cantidad y forma de pago.
  * Consultar su historial personal de reservas (Bitácora de consumo).
* **Restricciones:** Su límite de almuerzos es estricto (típicamente 2 por día). No tiene acceso a ningún área administrativa ni de cocina.

---

## 🛡️ Implementación Técnica en C#

### 1. Cookies y Claims
Cuando un usuario inicia sesión (`AuthService.cs` y `AccountController.cs`), el sistema crea una **Cookie de Autenticación**. Dentro de esta cookie viajan los **Claims** (Declaraciones o "Gafetes de identificación"). 
El sistema guarda en los *Claims* el ID del usuario, su nombre, su correo y su **Rol**.

### 2. Restricción de Pantallas (Controladores)
Para proteger una pantalla, utilizamos el atributo `[Authorize]` en los Controladores.
* **Ejemplo de acceso exclusivo:** 
  `[Authorize(Roles = "Administrador")]` encima de `AdminController` asegura que si un Empleado intenta entrar escribiendo la URL, el sistema lo expulsará.
* **Ejemplo de acceso compartido:**
  `[Authorize(Roles = "Empleado,Cocina,Administrador")]` encima de `EmpleadoController` permite que todos puedan ver el menú y hacer reservas.

### 3. Límites Dinámicos (Evitando Hardcoding)
El límite de platillos que un usuario puede pedir NO está fijo en el código (no hay `if (cantidad > 2)`). 
En su lugar, el `EmpleadoService.cs` lee la propiedad `max_almuerzos` directamente de la tabla `roles` vinculada al usuario. Esto significa que **para cambiar el límite de un rol, solo se debe actualizar la base de datos desde el panel de administración, sin tocar el código C#**.