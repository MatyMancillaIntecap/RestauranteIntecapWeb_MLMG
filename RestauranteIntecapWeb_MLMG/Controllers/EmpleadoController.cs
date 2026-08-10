using Microsoft.AspNetCore.Mvc;
using RestauranteIntecapWeb_MLMG.Models.DTOs;
using RestauranteIntecapWeb_MLMG.Services;

namespace RestauranteIntecapWeb_MLMG.Controllers
{
    public class EmpleadoController : Controller
    {
        private readonly IEmpleadoService _empleadoService;

        public EmpleadoController(IEmpleadoService empleadoService)
        {
            _empleadoService = empleadoService;
        }

        public async Task<IActionResult> Index()
        {
            int idUsuarioActivo = 3; // Usuario activo
            ViewBag.UsuarioId = idUsuarioActivo;

            var menuDisponible = await _empleadoService.ObtenerMenuDisponiblePorFechaAsync(DateTime.Today);
            return View(menuDisponible);
        }

        [HttpPost]
        public async Task<IActionResult> RealizarReserva([FromBody] SolicitudReservaDTO solicitud)
        {
            if (solicitud == null || solicitud.Platillos == null || !solicitud.Platillos.Any())
            {
                return BadRequest("Debe seleccionar al menos un platillo.");
            }

            solicitud.FechaConsumo = DateTime.Today;

            var (exito, mensaje) = await _empleadoService.ProcesarReservaAsync(solicitud);

            if (!exito)
            {
                return BadRequest(mensaje);
            }

            return Ok(new { mensaje = mensaje });
        }

        // Action POST para cancelar una reserva activa
        [HttpPost]
        public async Task<IActionResult> CancelarReserva(int reservaId)
        {
            int idUsuarioActivo = 3;
            var (exito, mensaje) = await _empleadoService.CancelarReservaAsync(reservaId, idUsuarioActivo);

            if (!exito)
            {
                return BadRequest(mensaje);
            }

            return Ok(new { mensaje = mensaje });
        }

        // Muestra el historial filtrado
        public async Task<IActionResult> Historial(DateTime? fechaInicio, DateTime? fechaFin, string? estado)
        {
            int idUsuarioActivo = 3;

            ViewBag.FechaInicio = fechaInicio?.ToString("yyyy-MM-dd");
            ViewBag.FechaFin = fechaFin?.ToString("yyyy-MM-dd");
            ViewBag.EstadoSel = estado ?? "Todos";

            var historial = await _empleadoService.ObtenerHistorialUsuarioFiltradoAsync(idUsuarioActivo, fechaInicio, fechaFin, estado);
            return View(historial);
        }

        // Descarga el Excel filtrado
        [HttpGet]
        public async Task<IActionResult> DescargarHistorialExcel(DateTime? fechaInicio, DateTime? fechaFin, string? estado)
        {
            int idUsuarioActivo = 3;
            byte[] bytesExcel = await _empleadoService.GenerarExcelHistorialFiltradoAsync(idUsuarioActivo, fechaInicio, fechaFin, estado);
            string nombreArchivo = $"Mi_Historial_Reservas_{DateTime.Now:yyyyMMdd}.xlsx";

            return File(bytesExcel, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", nombreArchivo);
        }

        // Descarga el PDF filtrado
        [HttpGet]
        public async Task<IActionResult> DescargarHistorialPdf(DateTime? fechaInicio, DateTime? fechaFin, string? estado)
        {
            int idUsuarioActivo = 3;
            byte[] bytesPdf = await _empleadoService.GenerarPdfHistorialFiltradoAsync(idUsuarioActivo, fechaInicio, fechaFin, estado);
            string nombreArchivo = $"Mi_Historial_Reservas_{DateTime.Now:yyyyMMdd}.pdf";

            return File(bytesPdf, "application/pdf", nombreArchivo);
        }
    }
}