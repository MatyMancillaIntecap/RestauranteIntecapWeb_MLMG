using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestauranteIntecapWeb_MLMG.Models.DTOs;
using RestauranteIntecapWeb_MLMG.Services;
using System.Security.Claims;

namespace RestauranteIntecapWeb_MLMG.Controllers
{
    // Habilitar acceso para Empleados, Cocina y Administradores
    [Authorize(Roles = "Empleado,Cocina,Administrador")]
    public class EmpleadoController : Controller
    {
        private readonly IEmpleadoService _empleadoService;

        public EmpleadoController(IEmpleadoService empleadoService)
        {
            _empleadoService = empleadoService;
        }





        // Consulta los platillos de hoy invocando el método exacto del contrato
        /* [HttpGet]
         public async Task<IActionResult> Index()
         {
             var idUsuarioClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
             int usuarioId = int.TryParse(idUsuarioClaim, out int id) ? id : 1;

             // Invocación del método firmado en IEmpleadoService.cs
             var menus = await _empleadoService.ObtenerMenuDisponiblePorFechaAsync(DateTime.Today);
             ViewBag.UsuarioId = usuarioId;



             // 1. Obtenemos el NIT del usuario desde la base de datos
             string nitPrecargado = await _empleadoService.ObtenerNitUsuarioAsync(usuarioId);

             // 2. Pasamos la información a la vista HTML mediante el ViewBag
             ViewBag.UsuarioId = usuarioId;
             ViewBag.NitUsuario = nitPrecargado;


             return View(menus);
         }*/


        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var idUsuarioClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int usuarioId = int.TryParse(idUsuarioClaim, out int id) ? id : 1;

            // 1. Obtenemos el menú disponible
            var menus = await _empleadoService.ObtenerMenuDisponiblePorFechaAsync(DateTime.Today);

            // 2. Pedimos el límite al servicio (arquitectura limpia)
            int limiteMaximoUsuario = await _empleadoService.ObtenerLimiteAlmuerzosUsuarioAsync(usuarioId);

            // 3. Pasamos todo a la vista mediante el ViewBag
            ViewBag.UsuarioId = usuarioId;
            ViewBag.LimiteMaximo = limiteMaximoUsuario;
            ViewBag.NitUsuario = await _empleadoService.ObtenerNitUsuarioAsync(usuarioId);

            return View(menus);
        }


        [HttpPost]
        public async Task<IActionResult> RealizarReserva([FromBody] SolicitudReservaDTO solicitud)
        {
            if (solicitud == null) return BadRequest("Solicitud no válida.");

            var (exito, mensaje) = await _empleadoService.ProcesarReservaAsync(solicitud);
            if (!exito)
            {
                return BadRequest(mensaje);
            }

            return Ok(new { mensaje });
        }

        // Acción para mostrar el historial de reservas simplificado (Consulta limpia)
        [HttpGet]
        public async Task<IActionResult> Historial(DateTime? fechaInicio, DateTime? fechaFin)
        {
            // Obtener el ID del usuario autenticado desde la cookie de sesión
            var idUsuarioClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int usuarioId = int.TryParse(idUsuarioClaim, out int id) ? id : 1;

            // Precargar las fechas por defecto con el día actual si no se envían filtros
            DateTime fInicio = fechaInicio ?? DateTime.Today;
            DateTime fFin = fechaFin ?? fInicio;

            ViewBag.FechaInicio = fInicio.ToString("yyyy-MM-dd");
            ViewBag.FechaFin = fFin.ToString("yyyy-MM-dd");

            // Consultar el servicio pasando "Todos" para ignorar el filtro de estado en la BD
            var historial = await _empleadoService.ObtenerHistorialUsuarioFiltradoAsync(usuarioId, fInicio, fFin, "Todos");

            return View(historial);
        }
        // ENDPOINT PARA DESCARGAR EL EXCEL
        [HttpGet]
        public async Task<IActionResult> DescargarExcelHistorial(DateTime? fechaInicio, DateTime? fechaFin)
        {
            // 1. Identificamos qué usuario está intentando descargar el reporte mediante su Cookie
            var idUsuarioClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int usuarioId = int.TryParse(idUsuarioClaim, out int id) ? id : 1;

            // 2. Pedimos al servicio que genere los bytes del archivo
            var archivoBytes = await _empleadoService.GenerarExcelHistorialFiltradoAsync(usuarioId, fechaInicio, fechaFin);

            // 3. Devolvemos el archivo web especificando el MIME Type oficial de Microsoft Excel
            return File(archivoBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"MiHistorial_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        // ENDPOINT PARA DESCARGAR EL PDF
        [HttpGet]
        public async Task<IActionResult> DescargarPdfHistorial(DateTime? fechaInicio, DateTime? fechaFin)
        {
            // 1. Identificamos al usuario
            var idUsuarioClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int usuarioId = int.TryParse(idUsuarioClaim, out int id) ? id : 1;

            // 2. Pedimos al servicio los bytes del PDF
            var archivoBytes = await _empleadoService.GenerarPdfHistorialFiltradoAsync(usuarioId, fechaInicio, fechaFin);

            // 3. Devolvemos el archivo usando el MIME Type oficial para PDFs
            return File(archivoBytes, "application/pdf", $"MiHistorial_{DateTime.Now:yyyyMMdd}.pdf");
        }
    }
}





