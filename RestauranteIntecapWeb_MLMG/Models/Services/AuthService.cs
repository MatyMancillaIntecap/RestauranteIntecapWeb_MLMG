// Importamos los espacios de nombres limpiando cualquier error de escritura inicial
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RestauranteIntecapWeb_MLMG.Data;
using RestauranteIntecapWeb_MLMG.Models;
using RestauranteIntecapWeb_MLMG.Models.DTOs;

namespace RestauranteIntecapWeb_MLMG.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly PasswordHasher<Usuario> _passwordHasher = new();

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

            // 4. Validación de contraseña con hash seguro
            var verificacion = _passwordHasher.VerifyHashedPassword(usuario, usuario.password, model.Password);

            if (verificacion == PasswordVerificationResult.Failed)
            {
                // Compatibilidad temporal con contraseñas heredadas en texto plano
                if (usuario.password != model.Password)
                {
                    return (false, "La contraseña ingresada es incorrecta.", null);
                }

                // Migración progresiva: al validar texto plano, se convierte inmediatamente a hash
                usuario.password = _passwordHasher.HashPassword(usuario, model.Password);
                await _context.SaveChangesAsync();
            }
            else if (verificacion == PasswordVerificationResult.SuccessRehashNeeded)
            {
                // Rehash automático con parámetros de seguridad actuales
                usuario.password = _passwordHasher.HashPassword(usuario, model.Password);
                await _context.SaveChangesAsync();
            }

            // 5. Registrar el acceso en la tabla historial_login
            await RegistrarAccesoAsync(usuario.id);

            return (true, "¡Inicio de sesión exitoso!", usuario);
        }

        // Crea una solicitud de restablecimiento de contraseña para un usuario existente
        public async Task<(bool Exito, string Mensaje)> CrearSolicitudRestablecimientoAsync(SolicitudRestablecimientoInputDTO model)
        {
            var correo = model.Identificador?.Trim().ToLower();

            if (string.IsNullOrWhiteSpace(correo) || !correo.Contains('@'))
            {
                return (false, "Ingresa el correo electrónico registrado para solicitar el restablecimiento.");
            }

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.email.ToLower() == correo);

            if (usuario == null)
            {
                return (false, "No se encontró un usuario con ese correo electrónico.");
            }

            if (!usuario.activo)
            {
                return (false, "Tu cuenta se encuentra desactivada. Contacta al Administrador.");
            }

            var solicitudPendiente = await _context.SolicitudesRestablecimientoPassword
                .AnyAsync(s => s.usuario_id == usuario.id && s.estado == "Pendiente");

            if (solicitudPendiente)
            {
                return (true, "Ya existe una solicitud pendiente para este usuario. El administrador la revisará pronto.");
            }

            var solicitud = new SolicitudRestablecimientoPassword
            {
                usuario_id = usuario.id,
                estado = "Pendiente",
                fecha_solicitud = DateTime.Now
            };

            _context.SolicitudesRestablecimientoPassword.Add(solicitud);
            await _context.SaveChangesAsync();

            return (true, "Tu solicitud fue enviada correctamente. Debes esperar a que un administrador gestione el cambio.");
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

        // Permite que un usuario autenticado cambie su propia contraseña
        public async Task<(bool Exito, string Mensaje)> CambiarContrasenaAsync(int usuarioId, string contrasenaActual, string nuevaContrasena, string confirmarContrasena)
        {
            // 1. Validaciones básicas
            if (string.IsNullOrWhiteSpace(contrasenaActual))
            {
                return (false, "La contraseña actual es obligatoria.");
            }

            if (string.IsNullOrWhiteSpace(nuevaContrasena))
            {
                return (false, "La nueva contraseña es obligatoria.");
            }

            if (string.IsNullOrWhiteSpace(confirmarContrasena))
            {
                return (false, "Debe confirmar la nueva contraseña.");
            }

            if (nuevaContrasena != confirmarContrasena)
            {
                return (false, "Las contraseñas nuevas no coinciden.");
            }

            if (nuevaContrasena.Length < 8)
            {
                return (false, "La nueva contraseña debe tener al menos 8 caracteres.");
            }

            // 2. Buscar el usuario
            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            if (usuario == null)
            {
                return (false, "Usuario no encontrado.");
            }

            // 3. Validar que la contraseña actual sea correcta
            var verificacion = _passwordHasher.VerifyHashedPassword(usuario, usuario.password, contrasenaActual);

            if (verificacion == PasswordVerificationResult.Failed)
            {
                // Compatibilidad temporal con texto plano
                if (usuario.password != contrasenaActual)
                {
                    return (false, "La contraseña actual es incorrecta.");
                }
            }

            // 4. Actualizar a la nueva contraseña (con hash)
            usuario.password = _passwordHasher.HashPassword(usuario, nuevaContrasena);
            await _context.SaveChangesAsync();

            return (true, "Tu contraseña ha sido cambiada correctamente.");
        }
    }
}
