using System.ComponentModel.DataAnnotations;

namespace RestauranteIntecapWeb_MLMG.Models.DTOs
{
    // DTO utilizado para transportar las credenciales ingresadas en la pantalla de Login
    public class LoginViewModel
    {
        [Required(ErrorMessage = "El correo electrónico o usuario es obligatorio.")]
        [EmailAddress(ErrorMessage = "Ingrese un correo electrónico válido.")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = null!;

        public bool Recordarme { get; set; } = false;
    }
}