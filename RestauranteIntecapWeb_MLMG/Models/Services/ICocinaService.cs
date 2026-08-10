// Importamos las entidades y DTOs desde sus espacios de nombres correspondientes
using RestauranteIntecapWeb_MLMG.Models;
using RestauranteIntecapWeb_MLMG.Models.DTOs;

namespace RestauranteIntecapWeb_MLMG.Services
{
    // Contrato que establece las operaciones backend obligatorias para el área de cocina
    public interface ICocinaService
    {
        // Obtiene todos los platillos agendados para una fecha
        Task<List<MenuDiario>> ObtenerMenusPorFechaAsync(DateTime fecha);

        // Busca un platillo específico por su identificador único
        Task<MenuDiario?> ObtenerMenuPorIdAsync(int id);

        // Guarda los cambios realizados en un platillo existente
        Task<bool> ActualizarMenuAsync(MenuDiario menu);

        // Cambia el estado (Disponible/Agotado/Inactivo) aplicando borrado lógico
        Task<bool> CambiarEstadoMenuAsync(int id, string nuevoEstado);

        // Intenta eliminar un platillo físicamente si no tiene reservas
        Task<(bool Exito, string Mensaje)> EliminarMenuSinReservasAsync(int id);

        // Retorna el detalle de personas que reservaron hoy
        Task<List<ReservaDetalleDTO>> ObtenerReservasDetalladasPorFechaAsync(DateTime fecha);

        // Retorna el conteo agrupado por platillo
        Task<List<PlatilloConsolidadoDTO>> ObtenerConsolidadoPorFechaAsync(DateTime fecha);

        // Genera la ráfaga de bytes del archivo de Excel (.xlsx) en memoria
        Task<byte[]> GenerarReporteExcelReservasAsync(DateTime fecha);

        // Genera la ráfaga de bytes del documento PDF en memoria listo para descarga
        Task<byte[]> GenerarReportePdfReservasAsync(DateTime fecha);
    }
}