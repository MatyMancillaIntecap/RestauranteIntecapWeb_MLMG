using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
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

        // Muestra la vista del Login
        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirigirSegunRol();
            }
            return View();
        }

        // Procesa el formulario de Inicio de Sesión
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var (exito, mensaje, usuario) = await _authService.ValidarCredencialesAsync(model);

            if (!exito || usuario == null)
            {
                ModelState.AddModelError("", mensaje);
                return View(model);
            }

            // Construir las declaraciones de identidad (Claims)
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

            // Crear la cookie de sesión en el navegador
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

    }



}