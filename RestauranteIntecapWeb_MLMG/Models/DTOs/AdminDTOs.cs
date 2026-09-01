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

    // DTO para listar las solicitudes de restablecimiento de contraseña
    public class SolicitudRestablecimientoPasswordDTO
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public string NombreUsuario { get; set; } = null!;
        public string EmailUsuario { get; set; } = null!;
        public DateTime FechaSolicitud { get; set; }
        public DateTime? FechaAtencion { get; set; }
        public string Estado { get; set; } = "Pendiente";
    }

    // DTO para que el usuario solicite el restablecimiento sin exponer contraseñas
    public class SolicitudRestablecimientoInputDTO
    {
        public string Identificador { get; set; } = null!;
    }

    // DTO para que el administrador asigne una nueva contraseña
    public class AtenderSolicitudRestablecimientoDTO
    {
        public int SolicitudId { get; set; }
        public string NuevaPassword { get; set; } = null!;
    }
}



