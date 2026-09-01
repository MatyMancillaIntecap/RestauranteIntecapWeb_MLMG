using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using RestauranteIntecapWeb_MLMG.Data;
using RestauranteIntecapWeb_MLMG.Models.Configuration;
using RestauranteIntecapWeb_MLMG.Services;
using QuestPDF.Infrastructure;

namespace RestauranteIntecapWeb_MLMG
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            QuestPDF.Settings.License = LicenseType.Community;

            builder.Services.AddControllersWithViews();

            // Configurar la conexión a SQL Server
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("ConexionSQL")));

            // Configuración de correo (se completa por appsettings o variables de entorno)
            builder.Services.Configure<CorreoOptions>(builder.Configuration.GetSection("Correo"));

            // Registrar los servicios de la aplicación
            builder.Services.AddScoped<ICorreoService, CorreoService>();
            builder.Services.AddScoped<AdminService>();
            builder.Services.AddScoped<IAdminService, AdminServiceConCorreo>();
            builder.Services.AddScoped<ICocinaService, CocinaService>();
            builder.Services.AddScoped<IEmpleadoService, EmpleadoService>();
            builder.Services.AddScoped<IAuthService, AuthService>();

            // Configurar Autenticación basada en Cookies
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Account/Login";
                    options.AccessDeniedPath = "/Account/AccesoDenegado";
                    options.ExpireTimeSpan = TimeSpan.FromHours(8); // Duración del pase de entrada
                });

            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();

            // Habilitar Seguridad
            app.UseAuthentication(); // Reconoce quién es el usuario
            app.UseAuthorization();  // Verifica qué permisos tiene

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Account}/{action=Login}/{id?}");

            app.Run();
        }
    }
}