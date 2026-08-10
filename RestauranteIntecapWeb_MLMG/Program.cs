// Importamos los espacios de nombres necesarios para Entity Framework Core y nuestras clases de datos
using Microsoft.EntityFrameworkCore;
using RestauranteIntecapWeb_MLMG.Data;

namespace RestauranteIntecapWeb_MLMG
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Crea el constructor de la aplicación web que carga la configuración de appsettings.json
            var builder = WebApplication.CreateBuilder(args);

            // Agrega el soporte para Controladores y Vistas (Arquitectura MVC) al contenedor de servicios
            builder.Services.AddControllersWithViews();

            // Registra la conexión a SQL Server utilizando la cadena 'ConexionSQL' configurada en appsettings.json
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("ConexionSQL")));

            // Construye la instancia de la aplicación con todos los servicios registrados
            var app = builder.Build();

            // Configura el manejo de errores en entorno de producción
            if (!app.Environment.IsDevelopment())
            {
                // Redirige a la vista de error por defecto si ocurre un fallo no controlado
                app.UseExceptionHandler("/Home/Error");
                // Habilita HSTS (Seguridad de transporte estricta por HTTP)
                app.UseHsts();
            }

            // Obliga a que todas las peticiones HTTP se redirijan a HTTPS (Conexión segura)
            app.UseHttpsRedirection();

            // Permite al servidor entregar archivos estáticos desde la carpeta wwwroot (imágenes, CSS, JS)
            app.UseStaticFiles();

            // Habilita el enrutamiento para mapear URLs a Controladores y Acciones
            app.UseRouting();

            // Habilita el control de autorizaciones y permisos de usuarios
            app.UseAuthorization();

            // Define la ruta predeterminada del sistema: NombreControlador/NombreAccion/IdOpcional
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            // Arranca el servidor web y se queda escuchando peticiones entrantes
            app.Run();
        }
    }
}