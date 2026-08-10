namespace RestauranteIntecapWeb_MLMG.Models.DTOs
{
    // DTO con las métricas clave para el Dashboard del Administrador
    public class DashboardDTO
    {
        public int TotalUsuarios { get; set; }
        public int UsuariosActivos { get; set; }
        public int UsuariosInactivos { get; set; }
        public int TotalReservasHoy { get; set; }
        public int ReservasActivasHoy { get; set; }
        public int ReservasCanceladasHoy { get; set; }
        public decimal TotalMontoRecaudadoHoy { get; set; }
        public string PlatilloMasVendido { get; set; } = "N/A";
        public int CantidadPlatilloMasVendido { get; set; }
        public int TotalPlatillosDisponibles { get; set; }
    }

    // DTO para la Ficha Detallada de un Usuario especifico
    public class DetalleUsuarioCompletoDTO
    {
        public UsuarioAdminDTO InfoUsuario { get; set; } = new UsuarioAdminDTO();
        public List<HistorialEmpleadoDTO> HistorialReservas { get; set; } = new List<HistorialEmpleadoDTO>();
        public decimal TotalGastadoAcumulado { get; set; }
        public int TotalPlatillosReservados { get; set; }
        public int TotalReservasCanceladas { get; set; }
    }

    // DTO para recibir los filtros de reportes globales
    public class FiltroReporteAdminDTO
    {
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public int? UsuarioId { get; set; }
        public string? Estado { get; set; }
        public int? MenuId { get; set; }
    }
}