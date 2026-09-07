using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RestauranteIntecapWeb_MLMG.Data;
using RestauranteIntecapWeb_MLMG.Models;
using RestauranteIntecapWeb_MLMG.Models.DTOs;
using RestauranteIntecapWeb_MLMG.Models.Services;

namespace RestauranteIntecapWeb_MLMG.Services
{
    public class AdminServiceConCorreo : IAdminService
    {
        private readonly AdminService _adminService;
        private readonly ApplicationDbContext _context;
        private readonly ICorreoService _correoService;
        private readonly PasswordHasher<Usuario> _passwordHasher = new();

        public AdminServiceConCorreo(AdminService adminService, ApplicationDbContext context, ICorreoService correoService)
        {
            _adminService = adminService;
            _context = context;
            _correoService = correoService;
        }

        public Task<List<UsuarioAdminDTO>> ObtenerTodosLosUsuariosAsync() => _adminService.ObtenerTodosLosUsuariosAsync();

        public Task<UsuarioEdicionDTO?> ObtenerUsuarioPorIdAsync(int id) => _adminService.ObtenerUsuarioPorIdAsync(id);

        public Task<(bool Exito, string Mensaje)> GuardarUsuarioAsync(UsuarioEdicionDTO usuarioDto) => _adminService.GuardarUsuarioAsync(usuarioDto);

        public Task<bool> CambiarEstadoUsuarioAsync(int id, bool activo) => _adminService.CambiarEstadoUsuarioAsync(id, activo);

        public Task<List<Rol>> ObtenerRolesAsync() => _adminService.ObtenerRolesAsync();

        public Task<int> ObtenerCantidadSolicitudesRestablecimientoPendientesAsync() => _adminService.ObtenerCantidadSolicitudesRestablecimientoPendientesAsync();

        public Task<List<SolicitudRestablecimientoPasswordDTO>> ObtenerSolicitudesRestablecimientoAsync() => _adminService.ObtenerSolicitudesRestablecimientoAsync();

        public async Task<(bool Exito, string Mensaje)> AtenderSolicitudRestablecimientoAsync(AtenderSolicitudRestablecimientoDTO dto, int adminUsuarioId)
        {
            if (dto == null)
            {
                return (false, "ERR: La solicitud es inválida.");
            }

            if (dto.SolicitudId <= 0)
            {
                return (false, "ERR: Debe seleccionar una solicitud válida.");
            }

            if (string.IsNullOrWhiteSpace(dto.NuevaPassword))
            {
                return (false, "ERR: Debe ingresar una contraseña nueva.");
            }

            if (adminUsuarioId <= 0)
            {
                return (false, "ERR: No se pudo identificar el administrador que atiende la solicitud.");
            }

            var solicitud = await _context.SolicitudesRestablecimientoPassword
                .Include(s => s.Usuario)
                .FirstOrDefaultAsync(s => s.id == dto.SolicitudId);

            if (solicitud == null)
            {
                return (false, "ERR: No se encontró la solicitud indicada.");
            }

            if (solicitud.estado != "Pendiente")
            {
                return (false, "ERR: La solicitud ya fue atendida previamente.");
            }

            if (solicitud.Usuario == null)
            {
                return (false, "ERR: No se encontró el usuario asociado a la solicitud.");
            }

            if (solicitud.usuario_id == adminUsuarioId)
            {
                return (false, "ERR: No puedes atender tu propia solicitud de contraseña.");
            }

            var destinatario = solicitud.Usuario.email?.Trim();
            if (string.IsNullOrWhiteSpace(destinatario))
            {
                return (false, "ERR: El usuario no tiene correo registrado.");
            }

            var usuario = solicitud.Usuario;
            usuario.password = _passwordHasher.HashPassword(usuario, dto.NuevaPassword);
            solicitud.estado = "Atendida";
            solicitud.fecha_atencion = DateTime.Now;
            solicitud.usuario_admin_id = adminUsuarioId;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {
                return (false, "ERR: No se pudo guardar la nueva contraseña.");
            }

            bool correoEnviado = await _correoService.EnviarRestablecimientoPasswordAsync(usuario, dto.NuevaPassword);
            if (!correoEnviado)
            {
                return (true, $"WARN: La contraseña se actualizó, pero no se pudo enviar correo a {destinatario}.");
            }

            return (true, $"OK: La contraseña se actualizó y SMTP aceptó el envío a {destinatario}. La entrega final depende del servidor de correo del destinatario.");
        }

        public Task<DashboardDTO> ObtenerMétricasDashboardAsync() => _adminService.ObtenerMétricasDashboardAsync();

        public Task<DetalleUsuarioCompletoDTO?> ObtenerDetalleCompletoUsuarioAsync(int usuarioId) => _adminService.ObtenerDetalleCompletoUsuarioAsync(usuarioId);

        public Task<byte[]> GenerarReporteGlobalExcelAsync(FiltroReporteAdminDTO filtro) => _adminService.GenerarReporteGlobalExcelAsync(filtro);

        public Task<byte[]> GenerarReporteGlobalPdfAsync(FiltroReporteAdminDTO filtro) => _adminService.GenerarReporteGlobalPdfAsync(filtro);

        public Task<byte[]> GenerarExcelUsuariosAsync() => _adminService.GenerarExcelUsuariosAsync();

        public Task<byte[]> GenerarPdfUsuariosAsync() => _adminService.GenerarPdfUsuariosAsync();
    }
}