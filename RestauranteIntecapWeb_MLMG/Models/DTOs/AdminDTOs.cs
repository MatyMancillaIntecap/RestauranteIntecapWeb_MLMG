namespace RestauranteIntecapWeb_MLMG.Models.DTOs
{
    // DTO para mostrar la lista general de usuarios en la tabla del Administrador
    public class UsuarioAdminDTO
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = null!;
        public string Email { get; set; } = null!;
        public int RolId { get; set; }
        public string NombreRol { get; set; } = null!;
        public bool Activo { get; set; }
        public string NitFacturacion { get; set; } = "C/F";
        public int MaxAlmuerzosPermitidos { get; set; } // Límite configurado del usuario
        public DateTime FechaCreacion { get; set; }
    }

    // DTO para crear o editar la información de un usuario
    public class UsuarioEdicionDTO
    {
        public int Id { get; set; } // 0 si es nuevo, >0 si es edición
        public string Nombre { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? Password { get; set; } // Opcional en edición
        public int RolId { get; set; }
        public bool Activo { get; set; } = true;
        public string NitFacturacion { get; set; } = "C/F";
        public int MaxAlmuerzos { get; set; } = 2; // Permite modificar el límite desde el modal
    }
}