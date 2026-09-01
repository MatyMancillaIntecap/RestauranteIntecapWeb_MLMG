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

    public class RecuperarPasswordViewModel
    {
        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "Ingrese un correo electrónico válido.")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "La nueva contraseña es obligatoria.")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "La nueva contraseña debe tener al menos 6 caracteres.")]
        public string NuevaPassword { get; set; } = null!;

        [Required(ErrorMessage = "Confirme la nueva contraseña.")]
        [DataType(DataType.Password)]
        [Compare(nameof(NuevaPassword), ErrorMessage = "Las contraseñas no coinciden.")]
        public string ConfirmarPassword { get; set; } = null!;
    }
}