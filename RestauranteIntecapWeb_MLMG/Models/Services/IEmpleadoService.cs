using RestauranteIntecapWeb_MLMG.Models;
using RestauranteIntecapWeb_MLMG.Models.DTOs;

namespace RestauranteIntecapWeb_MLMG.Services
{
    public interface IEmpleadoService
    {
        // Obtiene platillos disponibles para una fecha
        Task<List<MenuDiario>> ObtenerMenuDisponiblePorFechaAsync(DateTime fecha);

        // Procesa la reserva aplicando validaciones estrictas de limite
        Task<(bool Exito, string Mensaje)> ProcesarReservaAsync(SolicitudReservaDTO solicitud);

        // Cancela un platillo de una reserva, devuelve stock y valida fechas de corte
        Task<(bool Exito, string Mensaje)> CancelarReservaAsync(int reservaId, int usuarioId);

        // Obtiene el historial del usuario aplicando filtros por rango de fechas y estado
        Task<List<HistorialEmpleadoDTO>> ObtenerHistorialUsuarioFiltradoAsync(int usuarioId, DateTime? fechaInicio, DateTime? fechaFin, string? estado);

        Task<int> ObtenerLimiteAlmuerzosUsuarioAsync(int usuarioId);

        // Obtiene el NIT registrado del usuario actual
        Task<string> ObtenerNitUsuarioAsync(int usuarioId);

        // Genera el archivo Excel filtrado
        Task<byte[]> GenerarExcelHistorialFiltradoAsync(int usuarioId, DateTime? fechaInicio, DateTime? fechaFin);

        // Genera el archivo PDF filtrado
        Task<byte[]> GenerarPdfHistorialFiltradoAsync(int usuarioId, DateTime? fechaInicio, DateTime? fechaFin);






        // =========================================================================
        // NUEVOS MÉTODOS PARA ESTADÍSTICAS DEL PANEL (DASHBOARD Y RESUMEN)
        // =========================================================================
        Task<int> ObtenerPlatillosDietaSolicitadosHoyAsync(DateTime? fechaFiltro = null);
        Task<int> ObtenerPlatillosDietaInicialesHoyAsync(DateTime? fechaFiltro = null);
        Task<int> ObtenerPlatillosNormalesSolicitadosHoyAsync(DateTime? fechaFiltro = null);
        Task<int> ObtenerPlatillosNormalesInicialesHoyAsync(DateTime? fechaFiltro = null);
        Task<decimal> ObtenerVentasTotalesHoyAsync(DateTime? fechaFiltro = null);
        Task<int> ObtenerTotalReservasHoyAsync(DateTime? fechaFiltro = null);
        Task<int> ObtenerUsuariosConReservasHoyAsync(DateTime? fechaFiltro = null);
        Task<int> ObtenerTotalUsuariosRegistradosAsync();
    }
}