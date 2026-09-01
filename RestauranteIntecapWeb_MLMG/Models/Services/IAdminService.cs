using RestauranteIntecapWeb_MLMG.Models;
using RestauranteIntecapWeb_MLMG.Models.DTOs;

namespace RestauranteIntecapWeb_MLMG.Services
{
    public interface IAdminService
    {
        Task<List<UsuarioAdminDTO>> ObtenerTodosLosUsuariosAsync();
        Task<UsuarioEdicionDTO?> ObtenerUsuarioPorIdAsync(int id);
        Task<(bool Exito, string Mensaje)> GuardarUsuarioAsync(UsuarioEdicionDTO usuarioDto);
        Task<bool> CambiarEstadoUsuarioAsync(int id, bool activo);
        Task<List<Rol>> ObtenerRolesAsync();

        // Solicitudes de restablecimiento de contraseña
        Task<int> ObtenerCantidadSolicitudesRestablecimientoPendientesAsync();
        Task<List<SolicitudRestablecimientoPasswordDTO>> ObtenerSolicitudesRestablecimientoAsync();
        Task<(bool Exito, string Mensaje)> AtenderSolicitudRestablecimientoAsync(AtenderSolicitudRestablecimientoDTO dto);

        // NUEVOS MÉTODOS AVANZADOS PARA ADMINISTRACIÓN
        Task<DashboardDTO> ObtenerMétricasDashboardAsync();
        Task<DetalleUsuarioCompletoDTO?> ObtenerDetalleCompletoUsuarioAsync(int usuarioId);
        Task<byte[]> GenerarReporteGlobalExcelAsync(FiltroReporteAdminDTO filtro);
        Task<byte[]> GenerarReporteGlobalPdfAsync(FiltroReporteAdminDTO filtro);

        // Genera el reporte en Excel (.xlsx) de todos los usuarios registrados
        Task<byte[]> GenerarExcelUsuariosAsync();

        // Genera el reporte en PDF (.pdf) de todos los usuarios registrados
        Task<byte[]> GenerarPdfUsuariosAsync(); 
    }
}