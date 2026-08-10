using Microsoft.AspNetCore.Mvc;
using RestauranteIntecapWeb_MLMG.Models;
using RestauranteIntecapWeb_MLMG.Services;

namespace RestauranteIntecapWeb_MLMG.Controllers
{
    public class CocinaController : Controller
    {
        private readonly ICocinaService _cocinaService;
        private readonly IWebHostEnvironment _env;

        // Constructor con inyección del servicio de cocina y entorno web
        public CocinaController(ICocinaService cocinaService, IWebHostEnvironment env)
        {
            _cocinaService = cocinaService;
            _env = env;
        }

        // Muestra la vista principal con menús, consolidado y lista de reservas
        public async Task<IActionResult> Index(DateTime? fecha)
        {
            var fechaConsulta = fecha ?? DateTime.Today;
            ViewBag.FechaConsulta = fechaConsulta;

            var menus = await _cocinaService.ObtenerMenusPorFechaAsync(fechaConsulta);
            var consolidado = await _cocinaService.ObtenerConsolidadoPorFechaAsync(fechaConsulta);
            var reservasDetalle = await _cocinaService.ObtenerReservasDetalladasPorFechaAsync(fechaConsulta);

            ViewData["Consolidado"] = consolidado;
            ViewData["ReservasDetalle"] = reservasDetalle;

            return View(menus);
        }

        // Procesa la creación o edición de un platillo con fotografía
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarMenu(MenuDiario menu, IFormFile? imagenFile)
        {
            if (ModelState.IsValid)
            {
                if (imagenFile != null && imagenFile.Length > 0)
                {
                    var uploads = Path.Combine(_env.WebRootPath, "images", "menus");
                    if (!Directory.Exists(uploads))
                    {
                        Directory.CreateDirectory(uploads);
                    }

                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(imagenFile.FileName);
                    var filePath = Path.Combine(uploads, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imagenFile.CopyToAsync(stream);
                    }

                    menu.imagen_url = "/images/menus/" + fileName;
                }

                // Usamos el servicio de cocina para guardar o actualizar
                await _cocinaService.ActualizarMenuAsync(menu);

                return RedirectToAction(nameof(Index), new { fecha = menu.fecha.ToString("yyyy-MM-dd") });
            }

            return RedirectToAction(nameof(Index));
        }

        // Endpoint JSON para cargar datos en el modal de edición
        [HttpGet]
        public async Task<IActionResult> ObtenerMenuPorId(int id)
        {
            var menu = await _cocinaService.ObtenerMenuPorIdAsync(id);
            if (menu == null) return NotFound();

            return Json(menu);
        }

        // Cambia el estado (Disponible/Agotado/Inactivo)
        [HttpPost]
        public async Task<IActionResult> CambiarEstado(int id, string nuevoEstado)
        {
            var resultado = await _cocinaService.CambiarEstadoMenuAsync(id, nuevoEstado);
            if (!resultado) return BadRequest();

            return Ok();
        }

        // Elimina el platillo si no tiene reservas
        [HttpPost]
        public async Task<IActionResult> EliminarMenu(int id)
        {
            var (exito, mensaje) = await _cocinaService.EliminarMenuSinReservasAsync(id);
            if (!exito)
            {
                return BadRequest(mensaje);
            }

            return Ok(mensaje);
        }

        // Descarga el listado en formato Excel (.xlsx)
        [HttpGet]
        public async Task<IActionResult> DescargarExcel(DateTime fecha)
        {
            byte[] bytesExcel = await _cocinaService.GenerarReporteExcelReservasAsync(fecha);
            string nombreArchivo = $"Reservas_Cocina_{fecha:yyyyMMdd}.xlsx";

            return File(bytesExcel, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", nombreArchivo);
        }

        // Descarga el listado en formato PDF (.pdf)
        [HttpGet]
        public async Task<IActionResult> DescargarPdf(DateTime fecha)
        {
            byte[] bytesPdf = await _cocinaService.GenerarReportePdfReservasAsync(fecha);
            string nombreArchivo = $"Reporte_Reservas_{fecha:yyyyMMdd}.pdf";

            return File(bytesPdf, "application/pdf", nombreArchivo);
        }
    }
}