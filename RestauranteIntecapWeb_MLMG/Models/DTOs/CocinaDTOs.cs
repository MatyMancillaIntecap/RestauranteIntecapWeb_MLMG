namespace RestauranteIntecapWeb_MLMG.Models.DTOs
{
    // DTO para representar el recuento consolidado de un platillo en el día (Ej. Pollo en crema -> 3 solicitudes)
    public class PlatilloConsolidadoDTO
    {
        public int MenuId { get; set; }
        public string NombrePlato { get; set; } = null!;
        public decimal Precio { get; set; }
        public int TotalSolicitado { get; set; }
        public decimal TotalRecaudado { get; set; }
        public bool EsDieta { get; set; }
    }

    // DTO para representar el detalle individual de una persona que realizó una reserva
    public class ReservaDetalleDTO
    {
        public int ReservaId { get; set; }
        public string NombreEmpleado { get; set; } = null!;
        public string EmailEmpleado { get; set; } = null!;
        public string NombrePlato { get; set; } = null!;
        public int Cantidad { get; set; }
        public string DondeConsume { get; set; } = null!;
        public string FormaPago { get; set; } = null!;
        public DateTime FechaReserva { get; set; }
        public string Estado { get; set; } = null!;
    }
}