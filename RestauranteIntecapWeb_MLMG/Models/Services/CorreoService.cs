using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using RestauranteIntecapWeb_MLMG.Models.Configuration;

namespace RestauranteIntecapWeb_MLMG.Models.Services
{
    public class CorreoService : ICorreoService
    {
        private readonly CorreoOptions _opciones;
        private readonly ILogger<CorreoService> _logger;

        public CorreoService(IOptions<CorreoOptions> opciones, ILogger<CorreoService> logger)
        {
            _opciones = opciones.Value;
            _logger = logger;
        }

        public async Task<bool> EnviarCorreoNotificacionCambioAsync(Usuario usuario, string nuevaContrasenaPlano)
        {
            string mensajeHtml = $"<h3>Hola, {usuario.nombre}</h3>" +
                                 $"<p>Te informamos que tu contraseña ha sido actualizada exitosamente.</p>" +
                                 $"<p>Tu nueva contraseña en texto plano es: <b>{nuevaContrasenaPlano}</b></p>" +
                                 $"<p>Te recomendamos cambiarla al iniciar sesión.</p>";

            return await EnviarCorreoGenericoAsync(usuario.email, "Notificación de Cambio de Contraseña - Restaurante INTECAP", mensajeHtml);
        }

        public async Task<bool> EnviarRestablecimientoPasswordAsync(Usuario usuario, string nuevaPasswordTemporal)
        {
            string mensajeHtml = $"<h3>Hola, {usuario.nombre}</h3>" +
                                 $"<p>Has solicitado o un administrador ha reseteado tu contraseña.</p>" +
                                 $"<p>Tus nuevas credenciales temporales son: <b>{nuevaPasswordTemporal}</b></p>";

            return await EnviarCorreoGenericoAsync(usuario.email, "Notificación de Cambio de Contraseña...", mensajeHtml);
        }

        private async Task<bool> EnviarCorreoGenericoAsync(string destinatario, string asunto, string cuerpoHtml)
        {
            try
            {
                var remitente = string.IsNullOrWhiteSpace(_opciones.RemitenteCorreo)
                    ? _opciones.Usuario
                    : _opciones.RemitenteCorreo;

                if (string.IsNullOrWhiteSpace(_opciones.Host) ||
                    _opciones.Puerto <= 0 ||
                    string.IsNullOrWhiteSpace(remitente) ||
                    string.IsNullOrWhiteSpace(_opciones.Usuario) ||
                    string.IsNullOrWhiteSpace(_opciones.Contrasena) ||
                    string.IsNullOrWhiteSpace(destinatario))
                {
                    _logger.LogError("Configuración SMTP incompleta o destinatario vacío. Host:{Host} Puerto:{Puerto} UsuarioVacio:{UsuarioVacio} RemitenteVacio:{RemitenteVacio}",
                        _opciones.Host,
                        _opciones.Puerto,
                        string.IsNullOrWhiteSpace(_opciones.Usuario),
                        string.IsNullOrWhiteSpace(remitente));
                    return false;
                }

                MailAddress fromAddress;
                MailAddress toAddress;

                try
                {
                    fromAddress = new MailAddress(remitente, string.IsNullOrWhiteSpace(_opciones.RemitenteNombre) ? "Restaurante INTECAP" : _opciones.RemitenteNombre);
                    toAddress = new MailAddress(destinatario);
                }
                catch (FormatException ex)
                {
                    _logger.LogError(ex, "Formato de correo inválido. Remitente:{Remitente} Destinatario:{Destinatario}", remitente, destinatario);
                    return false;
                }

                using var mensaje = new MailMessage
                {
                    From = fromAddress,
                    Subject = asunto,
                    Body = cuerpoHtml,
                    IsBodyHtml = true,
                    DeliveryNotificationOptions = DeliveryNotificationOptions.OnFailure
                };

                mensaje.To.Add(toAddress);

                using var smtp = new SmtpClient(_opciones.Host, _opciones.Puerto)
                {
                    EnableSsl = _opciones.UsarSsl,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(_opciones.Usuario, _opciones.Contrasena),
                    Timeout = 30000
                };

                await smtp.SendMailAsync(mensaje);
                _logger.LogInformation("Correo aceptado por SMTP. De:{Remitente} Para:{Destinatario} Asunto:{Asunto}", remitente, destinatario, asunto);
                return true;
            }
            catch (SmtpException ex)
            {
                _logger.LogError(ex, "SMTP rechazó el correo. Codigo:{StatusCode} Mensaje:{Mensaje}", ex.StatusCode, ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR SMTP DETALLADO: {MensajeError}", ex.Message);
                System.Diagnostics.Debug.WriteLine($"EXCEPCIÓN SMTP REAL: {ex}");
                return false;
            }
        }
    }
}