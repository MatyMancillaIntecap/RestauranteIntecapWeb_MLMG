using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestauranteIntecapWeb_MLMG.Models
{
    // Tabla Roles
    [Table("roles")]
    public class Rol
    {
        [Key]
        public int id { get; set; }

        [Required]
        [StringLength(50)]
        public string nombre { get; set; } = null!;

        [StringLength(255)]
        public string? descripcion { get; set; }

        public int max_almuerzos { get; set; } = 2;
    }

    // Tabla Usuarios
    [Table("usuarios")]
    public class Usuario
    {
        [Key]
        public int id { get; set; }

        [Required]
        [StringLength(100)]
        public string nombre { get; set; } = null!;

        [Required]
        [StringLength(100)]
        public string email { get; set; } = null!;

        [Required]
        [StringLength(255)]
        public string password { get; set; } = null!;

        public int rol_id { get; set; }

        public bool activo { get; set; } = true;

        public DateTime fecha_creacion { get; set; } = DateTime.Now;

        [StringLength(20)]
        public string nit_facturacion { get; set; } = "C/F";

        [ForeignKey("rol_id")]
        public virtual Rol? Rol { get; set; }
    }

    // Tabla Formas de Pago
    [Table("formas_pago")]
    public class FormaPago
    {
        [Key]
        public int id { get; set; }

        [Required]
        [StringLength(50)]
        public string nombre { get; set; } = null!;
    }

    // Tabla Menú Diario
    [Table("menu_diario")]
    public class MenuDiario
    {
        [Key]
        public int id { get; set; }

        [Required]
        [StringLength(150)]
        public string nombre_plato { get; set; } = null!;

        [StringLength(1000)]
        public string? descripcion { get; set; }

        public decimal precio { get; set; }

        public int stock { get; set; }

        public int cantidad_solicitada { get; set; } = 0;

        [StringLength(255)]
        public string? imagen_url { get; set; }

        public DateTime fecha { get; set; }

        public bool es_dieta { get; set; } = false;

        [Required]
        [StringLength(20)]
        public string estado { get; set; } = "Disponible";
    }

    // Tabla Reservas
    [Table("reservas")]
    public class Reserva
    {
        [Key]
        public int id { get; set; }

        public int usuario_id { get; set; }

        public int menu_id { get; set; }

        public int forma_pago_id { get; set; }

        public int cantidad { get; set; }

        [Required]
        [StringLength(20)]
        public string donde_consume { get; set; } = "En restaurante";

        public DateTime fecha_reserva { get; set; } = DateTime.Now;

        public DateTime fecha_consumo { get; set; }

        [Required]
        [StringLength(20)]
        public string estado { get; set; } = "Activa";

        [StringLength(20)]
        public string nit_facturacion { get; set; } = "C/F";

        [ForeignKey("usuario_id")]
        public virtual Usuario? Usuario { get; set; }

        [ForeignKey("menu_id")]
        public virtual MenuDiario? MenuDiario { get; set; }

        [ForeignKey("forma_pago_id")]
        public virtual FormaPago? FormaPago { get; set; }
    }

    // Tabla Historial Login
    [Table("historial_login")]
    public class HistorialLogin
    {
        [Key]
        public int id { get; set; }

        public int usuario_id { get; set; }

        public DateTime fecha_login { get; set; } = DateTime.Now;

        [ForeignKey("usuario_id")]
        public virtual Usuario? Usuario { get; set; }
    }
}