# 🗄️ 2. Estructura de la Base de Datos

El sistema Restaurante Intecap utiliza **Microsoft SQL Server** como motor de base de datos relacional. La comunicación entre el código C# y SQL Server se realiza exclusivamente a través de **Entity Framework Core (ORM)**, utilizando el archivo `ApplicationDbContext.cs`.

## 📌 Diccionario de Tablas Principales

A continuación se detallan las tablas que componen el núcleo transaccional del sistema:

### 1. Tabla `roles`
* **Propósito:** Define los niveles de acceso del sistema y sus límites operativos.
* **Columnas clave:**
  * `id` (PK)
  * `nombre` (Ej: Administrador, Cocina, Empleado)
  * `max_almuerzos`: Define el límite dinámico de reservas permitidas por día. (0 = Ilimitado).

### 2. Tabla `usuarios`
* **Propósito:** Almacena las credenciales y datos personales del personal.
* **Relaciones:** Contiene `rol_id` (FK) que conecta con la tabla `roles`.
* **Columnas clave:** `email`, `password`, `nit_facturacion`, `activo`.
* ⚠️ **REGLA DE NEGOCIO (Soft Delete):** NUNCA se debe ejecutar un `DELETE` sobre un usuario. Para dar de baja a un empleado, el campo `activo` debe pasar a `false`. Esto garantiza que los reportes de reservas pasadas no se rompan.

### 3. Tabla `menu_diario`
* **Propósito:** Catálogo de platillos publicados por el área de cocina.
* **Columnas clave:**
  * `fecha`: Día exacto en el que el platillo estará disponible.
  * `stock`: Cantidad de porciones físicas disponibles. (Se reduce automáticamente al reservar).
  * `estado`: "Disponible" o "Agotado".

### 4. Tabla `formas_pago`
* **Propósito:** Catálogo de métodos de pago aceptados (Ej: Efectivo, Carnet).

### 5. Tabla `reservas` (Tabla Transaccional Principal)
* **Propósito:** Es el corazón financiero del sistema. Registra cada platillo solicitado.
* **Relaciones (Claves Foráneas):**
  * `usuario_id` -> Conecta con quién hizo la compra.
  * `menu_id` -> Conecta con el platillo comprado.
  * `forma_pago_id` -> Conecta con el método de pago utilizado.
* **Columnas clave:**
  * `cantidad`, `donde_consume` (Llevar / En restaurante).
  * `fecha_reserva`: Fecha y hora exacta en la que el usuario hizo clic en "Comprar".
  * `fecha_consumo`: Fecha para la cual está programado el platillo.
  * `estado`: "Activa", "Cancelada" o "Entregada".
* ⚠️ **REGLA DE NEGOCIO:** Si una reserva pasa a estado "Cancelada", la columna `cantidad` debe sumarse de vuelta al `stock` de la tabla `menu_diario` correspondiente.

### 6. Tabla `historial_login`
* **Propósito:** Tabla de auditoría de seguridad. Registra automáticamente cada vez que un usuario inicia sesión en el sistema.
* **Relaciones:** `usuario_id` (FK).
* **Columnas clave:** `fecha_login`.

---

## 🗺️ Diagrama Mental de Relaciones

```text
[roles] 1 <----- N [usuarios] 1 <----- N [historial_login]
                       │
                       │ 1
                       │
                       ▼ N
[menu_diario] 1 <--- [reservas] N ---> 1 [formas_pago]








