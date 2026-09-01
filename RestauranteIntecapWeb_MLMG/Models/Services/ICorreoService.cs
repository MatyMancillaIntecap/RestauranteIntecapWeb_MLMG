using RestauranteIntecapWeb_MLMG.Models;

namespace RestauranteIntecapWeb_MLMG.Services
{
    public interface ICorreoService
    {
        Task<(bool Exito, string Mensaje)> EnviarRestablecimientoPasswordAsync(Usuario usuario, string nuevaPasswordTemporal);
    }
}