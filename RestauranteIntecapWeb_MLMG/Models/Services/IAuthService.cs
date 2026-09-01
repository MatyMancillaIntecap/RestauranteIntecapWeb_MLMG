using RestauranteIntecapWeb_MLMG.Models;
using RestauranteIntecapWeb_MLMG.Models.DTOs;

namespace RestauranteIntecapWeb_MLMG.Services
{
    public interface IAuthService
    {
        // Valida el correo y contraseña contra SQL Server y registra el inicio en historial_login
        Task<(bool Exito, string Mensaje, Usuario? Usuario)> ValidarCredencialesAsync(LoginViewModel model);

        // Registra el acceso del usuario en la tabla historial_login
        Task RegistrarAccesoAsync(int usuarioId);

        // Crea una solicitud de restablecimiento de contraseña para un usuario existente
        Task<(bool Exito, string Mensaje)> CrearSolicitudRestablecimientoAsync(SolicitudRestablecimientoInputDTO model);
    }
}