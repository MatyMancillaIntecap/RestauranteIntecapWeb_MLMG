namespace RestauranteIntecapWeb_MLMG.Models.Configuration
{
    public class CorreoOptions
    {
        public string Host { get; set; } = string.Empty;
        public int Puerto { get; set; } = 587;
        public bool UsarSsl { get; set; } = true;
        public string Usuario { get; set; } = string.Empty;
        public string Contrasena { get; set; } = string.Empty;
        public string RemitenteCorreo { get; set; } = string.Empty;
        public string RemitenteNombre { get; set; } = "Restaurante Escuela INTECAP";
    }
}