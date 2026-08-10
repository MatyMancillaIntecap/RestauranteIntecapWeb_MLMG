using Microsoft.EntityFrameworkCore;
using RestauranteIntecapWeb_MLMG.Models;

namespace RestauranteIntecapWeb_MLMG.Data
{
    // Clase principal de contexto de Entity Framework que gestiona la base de datos
    public class ApplicationDbContext : DbContext
    {
        // Constructor que recibe las opciones de configuración (como la cadena de conexión)
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Definición de las colecciones que representan cada tabla en SQL Server
        public DbSet<Rol> Roles { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<FormaPago> FormasPago { get; set; }
        public DbSet<MenuDiario> MenusDiarios { get; set; }
        public DbSet<Reserva> Reservas { get; set; }
    }
}