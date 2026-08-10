// Importamos los espacios de nombres limpiando cualquier error de escritura inicial
using Microsoft.EntityFrameworkCore;
using RestauranteIntecapWeb_MLMG.Data;
using RestauranteIntecapWeb_MLMG.Models;
using RestauranteIntecapWeb_MLMG.Models.DTOs;

namespace RestauranteIntecapWeb_MLMG.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;

        // Inyección de dependencias del DbContext
        public AuthService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Validamos correo, cuenta activa y contraseña contra SQL Server
        public async Task<(bool Exito, string Mensaje, Usuario? Usuario)> ValidarCredencialesAsync(LoginViewModel model)
        {
            // 1. Buscar el usuario por su correo electrónico e incluir la tabla del Rol (.Include)
            var usuario = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.email.ToLower() == model.Email.Trim().ToLower());

            // 2. Validación de existencia
            if (usuario == null)
            {
                return (false, "El correo electrónico ingresado no se encuentra registrado.", null);
            }

            // 3. Validación de estado de cuenta (Soft Delete)
            if (!usuario.activo)
            {
                return (false, "Tu cuenta se encuentra desactivada. Contacta al Administrador.", null);
            }

            // 4. Validación de contraseña
            if (usuario.password != model.Password)
            {
                return (false, "La contraseña ingresada es incorrecta.", null);
            }

            // 5. Registrar el acceso en la tabla historial_login
            await RegistrarAccesoAsync(usuario.id);

            return (true, "¡Inicio de sesión exitoso!", usuario);
        }

        // Inserta el registro de auditoría en la tabla historial_login de SQL Server
        public async Task RegistrarAccesoAsync(int usuarioId)
        {
            var historial = new HistorialLogin
            {
                usuario_id = usuarioId,
                fecha_login = DateTime.Now
            };

            _context.HistorialLogins.Add(historial);
            await _context.SaveChangesAsync();
        }
    }
}