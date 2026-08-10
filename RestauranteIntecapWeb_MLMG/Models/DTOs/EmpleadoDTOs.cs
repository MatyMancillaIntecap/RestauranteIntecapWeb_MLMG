namespace RestauranteIntecapWeb_MLMG.Models.DTOs
{
    // DTO que representa un elemento individual seleccionado por el empleado para reservar
    public class ItemReservaDTO
    {
        public int MenuId { get; set; }
        public int Cantidad { get; set; }
        public int FormaPagoId { get; set; } // Pago individual por platillo (1: Efectivo, 2: Carnet)
        public string DondeConsume { get; set; } = "En restaurante";
    }

    // DTO contenedor enviado al backend con la lista de platillos y el NIT de facturación
    public class SolicitudReservaDTO
    {
        public int UsuarioId { get; set; }
        public DateTime FechaConsumo { get; set; }
        public string NitFacturacion { get; set; } = "C/F"; // NIT enviado para esta compra
        public List<ItemReservaDTO> Platillos { get; set; } = new List<ItemReservaDTO>();
    }

    // DTO estructurado para las filas del historial personal
    public class HistorialEmpleadoDTO
    {
        public int ReservaId { get; set; }
        public DateTime FechaConsumo { get; set; }
        public string NombrePlato { get; set; } = null!;
        public string ImagenUrl { get; set; } = null!;
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal TotalPagado => Cantidad * PrecioUnitario;
        public string FormaPago { get; set; } = null!;
        public string DondeConsume { get; set; } = null!;
        public string NitFacturacion { get; set; } = "C/F";
        public string Estado { get; set; } = null!;
        public DateTime FechaReserva { get; set; }
    }
}