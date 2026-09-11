using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestauranteIntecapWeb_MLMG.Models.DTOs;
using RestauranteIntecapWeb_MLMG.Services;

namespace RestauranteIntecapWeb_MLMG.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;

        public AccountController(IAuthService authService)
        {
            _authService = authService;
        }

        // Muestra la vista del Login (GET)
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // Procesa el formulario de Inicio de Sesión (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Validamos las credenciales utilizando el contrato de servicio existente
            var (exito, mensaje, usuario) = await _authService.ValidarCredencialesAsync(model);

            if (!exito || usuario == null)
            {
                ModelState.AddModelError(string.Empty, mensaje ?? "Correo electrónico o contraseña incorrectos.");
                return View(model);
            }

            // Construir las declaraciones de identidad (Claims) para la sesión
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.id.ToString()),
                new Claim(ClaimTypes.Name, usuario.nombre),
                new Claim(ClaimTypes.Email, usuario.email),
                new Claim(ClaimTypes.Role, usuario.Rol!.nombre)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.Recordarme
            };

            // Crear la cookie de sesión cifrada en el navegador
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            return RedirigirSegunRol(usuario.Rol.nombre);
        }

        // Cierra la sesión activa
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        // Muestra pantalla amigable si intenta entrar a un área sin permisos
        [HttpGet]
        public IActionResult AccesoDenegado()
        {
            return View();
        }

        // Método auxiliar para redirigir según el rol del usuario
        private IActionResult RedirigirSegunRol(string? nombreRol = null)
        {
            var rol = nombreRol ?? User.FindFirstValue(ClaimTypes.Role);

            return rol switch
            {
                "Administrador" => RedirectToAction("Index", "Admin"),
                "Cocina" => RedirectToAction("Index", "Cocina"),
                _ => RedirectToAction("Index", "Empleado")
            };
        }

        // Muestra la vista para solicitar el restablecimiento de contraseña (GET)
        [HttpGet]
        public IActionResult RecuperarPassword()
        {
            return View(new SolicitudRestablecimientoInputDTO());
        }

        // Procesa la solicitud de restablecimiento de contraseña (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecuperarPassword(SolicitudRestablecimientoInputDTO model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var (exito, mensaje) = await _authService.CrearSolicitudRestablecimientoAsync(model);

            if (!exito)
            {
                ModelState.AddModelError(string.Empty, mensaje);
                return View(model);
            }

            ViewBag.Mensaje = mensaje;
            ModelState.Clear();
            return View(new SolicitudRestablecimientoInputDTO());
        }

        // Muestra el formulario para cambiar contraseña (GET) - Solo para usuarios autenticados
        [Authorize]
        [HttpGet]
        public IActionResult CambiarPassword()
        {
            return View(new CambiarContrasenaDTO());
        }

        // Procesa el cambio de contraseña (POST) - Solo para usuarios autenticados
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarPassword(CambiarContrasenaDTO model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Obtener el ID del usuario autenticado
            var usuarioIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(usuarioIdClaim, out var usuarioId) || usuarioId <= 0)
            {
                TempData["Error"] = "No se pudo identificar tu usuario autenticado.";
                return View(model);
            }

            // Llamar al servicio para cambiar la contraseña
            var (exito, mensaje) = await _authService.CambiarContrasenaAsync(
                usuarioId,
                model.ContrasenaActual,
                model.NuevaContrasena,
                model.ConfirmarContrasena);

            if (!exito)
            {
                TempData["Error"] = mensaje;
                return View(model);
            }

            // Si el cambio fue exitoso, guardar mensaje y redirigir según el rol
            TempData["Exito"] = mensaje;
            var rolUsuario = User.FindFirstValue(ClaimTypes.Role);

            return rolUsuario switch
            {
                "Administrador" => RedirectToAction("Index", "Admin"),
                "Cocina" => RedirectToAction("Index", "Cocina"),
                _ => RedirectToAction("Index", "Empleado")
            };
        }
    }
}