using Microsoft.AspNetCore.Mvc;
using RestauranteIntecapWeb_MLMG.Models.DTOs;
using RestauranteIntecapWeb_MLMG.Services;

namespace RestauranteIntecapWeb_MLMG.Controllers
{
    public class AdminController : Controller
    {
        private readonly IAdminService _adminService;
        private readonly ICocinaService _cocinaService;

        public AdminController(IAdminService adminService, ICocinaService cocinaService)
        {
            _adminService = adminService;
            _cocinaService = cocinaService;
        }

        // Muestra el Dashboard Principal con KPIs
        public async Task<IActionResult> Index()
        {
            return View();
        }

        // Muestra la lista de usuarios
        public async Task<IActionResult> Usuarios()
        {
            var usuarios = await _adminService.ObtenerTodosLosUsuariosAsync();
            ViewBag.Roles = await _adminService.ObtenerRolesAsync();
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
    }
}