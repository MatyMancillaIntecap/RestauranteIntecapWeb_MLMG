// Importamos los paquetes necesarios para trabajar con peticiones Web y Entity Framework
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestauranteIntecapWeb_MLMG.Data;
using RestauranteIntecapWeb_MLMG.Models;

namespace RestauranteIntecapWeb_MLMG.Controllers
{
    public class CocinaController : Controller
    {
        // Declaramos variables privadas e inmutables para la base de datos y el entorno web
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        // El constructor recibe por inyección de dependencias el contexto de datos y el acceso a carpetas del servidor
        public CocinaController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // Método que consulta y muestra los menús registrados para el día actual
        public async Task<IActionResult> Index()
        {
            // Obtenemos únicamente la fecha de hoy sin hora
            var hoy = DateTime.Today;

            // Consultamos la tabla menu_diario filtrando los platillos cuya fecha coincida con el día de hoy
            var menusHoy = await _context.MenusDiarios
                .Where(m => m.fecha == hoy)
                .ToListAsync();

            // Enviamos la lista de platillos a la vista correspondiente
            return View(menusHoy);
        }

        // Método POST que recibe los datos del formulario para guardar un nuevo plato en el menú
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarMenu(MenuDiario menu, IFormFile? imagenFile)
        {
            // Verifica que los datos recibidos cumplan con las reglas del modelo
            if (ModelState.IsValid)
            {
                try
                {
                    // Si el usuario seleccionó un archivo de imagen en el formulario
                    if (imagenFile != null && imagenFile.Length > 0)
                    {
                        // Construye la ruta física absoluta hacia la carpeta wwwroot/images/menus
                        var uploads = Path.Combine(_env.WebRootPath, "images", "menus");

                        // Si la carpeta física no existe en el disco, la crea automáticamente
                        if (!Directory.Exists(uploads))
                        {
                            Directory.CreateDirectory(uploads);
                        }

                        // Genera un nombre único global (GUID) conservando la extensión del archivo (.jpg, .png)
                        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(imagenFile.FileName);
                        var filePath = Path.Combine(uploads, fileName);

                        // Copia y guarda la imagen en el almacenamiento del servidor de forma asíncrona
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await imagenFile.CopyToAsync(stream);
                        }

                        // Guarda la URL relativa accesible por el navegador en la base de datos
                        menu.imagen_url = "/images/menus/" + fileName;
                    }

                    // Agrega el nuevo objeto menú a la entidad de Entity Framework
                    _context.MenusDiarios.Add(menu);

                    // Ejecuta la consulta INSERT de SQL Server
                    await _context.SaveChangesAsync();

                    // Redirige al usuario nuevamente a la lista principal de cocina
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    // Captura cualquier error de base de datos y lo agrega al estado del modelo
                    ModelState.AddModelError("", "Error al guardar el menú: " + ex.Message);
                }
            }

            // Si el formulario tenía errores, vuelve a cargar la vista con la lista actual
            return View("Index", await _context.MenusDiarios.ToListAsync());
        }
    }
}