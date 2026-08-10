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

        // Genera el archivo Excel filtrado
        Task<byte[]> GenerarExcelHistorialFiltradoAsync(int usuarioId, DateTime? fechaInicio, DateTime? fechaFin, string? estado);

        // Genera el archivo PDF filtrado
        Task<byte[]> GenerarPdfHistorialFiltradoAsync(int usuarioId, DateTime? fechaInicio, DateTime? fechaFin, string? estado);
    }
}