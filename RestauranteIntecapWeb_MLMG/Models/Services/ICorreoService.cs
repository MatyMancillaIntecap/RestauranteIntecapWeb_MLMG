using RestauranteIntecapWeb_MLMG.Models;

namespace RestauranteIntecapWeb_MLMG.Models.Services
{
    public interface ICorreoService
    {
        Task<bool> EnviarCorreoNotificacionCambioAsync(Usuario usuario, string nuevaContrasenaPlano);
        Task<bool> EnviarRestablecimientoPasswordAsync(Usuario usuario, string nuevaPasswordTemporal);
    }
}