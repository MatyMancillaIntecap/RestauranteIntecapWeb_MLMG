using Microsoft.EntityFrameworkCore;
using RestauranteIntecapWeb_MLMG.Models;

namespace RestauranteIntecapWeb_MLMG.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        // Declaración de las tablas mapeadas en la base de datos Intecap_proy_Rest_m
        public DbSet<Rol> Roles { get; set; } = null!;
        public DbSet<Usuario> Usuarios { get; set; } = null!;
        public DbSet<FormaPago> FormasPago { get; set; } = null!;
        public DbSet<MenuDiario> MenusDiarios { get; set; } = null!;
        public DbSet<Reserva> Reservas { get; set; } = null!;
        public DbSet<HistorialLogin> HistorialLogins { get; set; } = null!;
        public DbSet<SolicitudRestablecimientoPassword> SolicitudesRestablecimientoPassword { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Rol>().ToTable("roles");
            modelBuilder.Entity<Usuario>().ToTable("usuarios");
            modelBuilder.Entity<FormaPago>().ToTable("formas_pago");
            modelBuilder.Entity<MenuDiario>().ToTable("menu_diario");
            modelBuilder.Entity<Reserva>().ToTable("reservas");
            modelBuilder.Entity<HistorialLogin>().ToTable("historial_login");
            modelBuilder.Entity<SolicitudRestablecimientoPassword>().ToTable("solicitudes_restablecimiento_password");
        }
    }
}