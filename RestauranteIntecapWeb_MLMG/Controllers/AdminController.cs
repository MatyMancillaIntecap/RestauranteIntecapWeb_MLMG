using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestauranteIntecapWeb_MLMG.Models.DTOs;
using RestauranteIntecapWeb_MLMG.Services;

namespace RestauranteIntecapWeb_MLMG.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class AdminController : Controller
    {
        private readonly IAdminService _adminService;
        private readonly ICocinaService _cocinaService;
        private readonly IEmpleadoService _empleadoService;

        public AdminController(IAdminService adminService, ICocinaService cocinaService, IEmpleadoService empleadoService)
        {
            _adminService = adminService;
            _cocinaService = cocinaService;
            _empleadoService = empleadoService;
        }

        // Muestra el Dashboard Principal con KPIs
        public async Task<IActionResult> Index(DateTime? fechaFiltro)
        {
            // Llamamos a los métodos del servicio que creamos previamente, 
            // pasando opcionalmente el filtro de fecha seleccionado por el administrador.
            ViewBag.DietaSolicitados = await _empleadoService.ObtenerPlatillosDietaSolicitadosHoyAsync(fechaFiltro);
            ViewBag.DietaIniciales = await _empleadoService.ObtenerPlatillosDietaInicialesHoyAsync(fechaFiltro);

            ViewBag.NormalesSolicitados = await _empleadoService.ObtenerPlatillosNormalesSolicitadosHoyAsync(fechaFiltro);
            ViewBag.NormalesIniciales = await _empleadoService.ObtenerPlatillosNormalesInicialesHoyAsync(fechaFiltro);

            ViewBag.VentasHoy = await _empleadoService.ObtenerVentasTotalesHoyAsync(fechaFiltro);
            ViewBag.ReservasHoy = await _empleadoService.ObtenerTotalReservasHoyAsync(fechaFiltro);

            int usuariosConReserva = await _empleadoService.ObtenerUsuariosConReservasHoyAsync(fechaFiltro);
            int totalUsuarios = await _empleadoService.ObtenerTotalUsuariosRegistradosAsync();

            // Formateamos el texto tal como lo pediste: "X con reservas hoy / Y registrados"
            ViewBag.TextoUsuarios = $"{usuariosConReserva} con reservas hoy / {totalUsuarios} registrados";
            ViewBag.SolicitudesPasswordPendientes = await _adminService.ObtenerCantidadSolicitudesRestablecimientoPendientesAsync();

            // Mantenemos la fecha actual o seleccionada para el formulario de filtro en la vista
            ViewBag.FechaFiltroSeleccionada = fechaFiltro?.ToString("yyyy-MM-dd") ?? DateTime.Today.ToString("yyyy-MM-dd");

            return View();
        }

        // Muestra la lista de usuarios
        public async Task<IActionResult> Usuarios()
        {
            var usuarios = await _adminService.ObtenerTodosLosUsuariosAsync();
            ViewBag.Roles = await _adminService.ObtenerRolesAsync();
            ViewBag.SolicitudesPasswordPendientes = await _adminService.ObtenerCantidadSolicitudesRestablecimientoPendientesAsync();
            return View(usuarios);
        }

        // Muestra la ficha detallada de un usuario específico
        public async Task<IActionResult> DetalleUsuario(int id)
        {
            var detalle = await _adminService.ObtenerDetalleCompletoUsuarioAsync(id);
            if (detalle == null) return NotFound();

            return View(detalle);
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerUsuarioPorId(int id)
        {
            var usuario = await _adminService.ObtenerUsuarioPorIdAsync(id);
            if (usuario == null) return NotFound();
            return Json(usuario);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarUsuario(UsuarioEdicionDTO dto)
        {
            var (exito, mensaje) = await _adminService.GuardarUsuarioAsync(dto);
            if (!exito)
            {
                TempData["Error"] = mensaje;
            }
            else
            {
                TempData["Exito"] = mensaje;
            }

            return RedirectToAction(nameof(Usuarios));
        }

        [HttpPost]
        public async Task<IActionResult> CambiarEstadoUsuario(int id, bool activo)
        {
            var resultado = await _adminService.CambiarEstadoUsuarioAsync(id, activo);
            if (!resultado) return BadRequest();
            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> SolicitudesRestablecimiento()
        {
            var solicitudes = await _adminService.ObtenerSolicitudesRestablecimientoAsync();
            ViewBag.SolicitudesPasswordPendientes = await _adminService.ObtenerCantidadSolicitudesRestablecimientoPendientesAsync();
            return View(solicitudes);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AtenderSolicitudRestablecimiento(AtenderSolicitudRestablecimientoDTO dto)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Debe completar la contraseña nueva antes de atender la solicitud.";
                return RedirectToAction(nameof(SolicitudesRestablecimiento));
            }

            var solicitud = (await _adminService.ObtenerSolicitudesRestablecimientoAsync())
                .FirstOrDefault(s => s.Id == dto.SolicitudId);

            var (exito, mensaje) = await _adminService.AtenderSolicitudRestablecimientoAsync(dto);

            if (!exito)
            {
                TempData["Error"] = LimpiarPrefijoMensaje(mensaje);
                return RedirectToAction(nameof(SolicitudesRestablecimiento));
            }

            TempData["Exito"] = LimpiarPrefijoMensaje(mensaje);

            if (solicitud != null)
            {
                TempData["CorreoAsunto"] = "Restablecimiento de contraseña – Restaurante Escuela INTECAP";
                TempData["CorreoCuerpo"] = ConstruirCorreoCopiable(solicitud.NombreUsuario, solicitud.EmailUsuario, dto.NuevaPassword);
            }

            return RedirectToAction(nameof(SolicitudesRestablecimiento));
        }

        private static string ConstruirCorreoCopiable(string nombreUsuario, string correoUsuario, string nuevaPassword)
        {
            return $"Estimado/a {nombreUsuario}:\n\n" +
                   "Le informamos que su contraseña de acceso al sistema Restaurante Escuela INTECAP ha sido restablecida exitosamente por el administrador.\n\n" +
                   "Sus nuevas credenciales de acceso son:\n\n" +
                   $"Usuario: {correoUsuario}\n" +
                   $"Contraseña temporal: {nuevaPassword}\n\n" +
                   "Le recomendamos conservar estas credenciales en un lugar seguro y, si el sistema lo permite, cambiar su contraseña posteriormente.\n\n" +
                   "Si usted no solicitó este cambio, por favor comuníquese con el administrador del sistema.\n\n" +
                   "Saludos cordiales,\n" +
                   "Administración\n" +
                   "Restaurante Escuela INTECAP";
        }

        private static string LimpiarPrefijoMensaje(string mensaje)
        {
            if (mensaje.StartsWith("OK:"))
            {
                return mensaje[3..].Trim();
            }

            if (mensaje.StartsWith("WARN:"))
            {
                return mensaje[5..].Trim();
            }

            if (mensaje.StartsWith("ERR:"))
            {
                return mensaje[4..].Trim();
            }

            return mensaje;
        }

        // Action para exportar reporte Excel global filtrado
        [HttpGet]
        public async Task<IActionResult> DescargarReporteExcel([FromQuery] FiltroReporteAdminDTO filtro)
        {
            byte[] bytesExcel = await _adminService.GenerarReporteGlobalExcelAsync(filtro);
            string nombreArchivo = $"Reporte_Admin_Global_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";

            return File(bytesExcel, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", nombreArchivo);
        }

        // Action para exportar reporte PDF global filtrado
        [HttpGet]
        public async Task<IActionResult> DescargarReportePdf([FromQuery] FiltroReporteAdminDTO filtro)
        {
            byte[] bytesPdf = await _adminService.GenerarReporteGlobalPdfAsync(filtro);
            string nombreArchivo = $"Reporte_Admin_Global_{DateTime.Now:yyyyMMdd_HHmm}.pdf";

            return File(bytesPdf, "application/pdf", nombreArchivo);
        }



        // Endpoint GET para descargar la lista completa de usuarios en Excel
        [HttpGet]
        public async Task<IActionResult> DescargarUsuariosExcel()
        {
            byte[] bytesExcel = await _adminService.GenerarExcelUsuariosAsync();
            string nombreArchivo = $"Usuarios_Sistema_{DateTime.Now:yyyyMMdd}.xlsx";

            return File(bytesExcel, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", nombreArchivo);
        }

        // Endpoint GET para descargar la lista completa de usuarios en PDF
        [HttpGet]
        public async Task<IActionResult> DescargarUsuariosPdf()
        {
            byte[] bytesPdf = await _adminService.GenerarPdfUsuariosAsync();
            string nombreArchivo = $"Padron_Usuarios_{DateTime.Now:yyyyMMdd}.pdf";

            return File(bytesPdf, "application/pdf", nombreArchivo);
        }








        [HttpGet]
        public async Task<IActionResult> DescargarExcelAdmin(DateTime? fechaFiltro)
        {
            // Reutilizamos la lógica del servicio para generar los bytes del Excel filtrado por fecha
            var archivoBytes = await _empleadoService.GenerarExcelHistorialFiltradoAsync(0, fechaFiltro, fechaFiltro);
            return File(archivoBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"ReporteAdmin_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        [HttpGet]
        public async Task<IActionResult> DescargarPdfAdmin(DateTime? fechaFiltro)
        {
            var archivoBytes = await _empleadoService.GenerarPdfHistorialFiltradoAsync(0, fechaFiltro, fechaFiltro);
            return File(archivoBytes, "application/pdf", $"ReporteAdmin_{DateTime.Now:yyyyMMdd}.pdf");
        }

    }
}