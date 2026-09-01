using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Options;
using RestauranteIntecapWeb_MLMG.Models;
using RestauranteIntecapWeb_MLMG.Models.Configuration;

namespace RestauranteIntecapWeb_MLMG.Services
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

        public async Task<(bool Exito, string Mensaje)> EnviarRestablecimientoPasswordAsync(Usuario usuario, string nuevaPasswordTemporal)
        {
            if (usuario == null)
            {
                return (false, "No se encontró la información del usuario para enviar el correo.");
            }

            if (string.IsNullOrWhiteSpace(usuario.email))
            {
                return (false, "El usuario no tiene un correo electrónico registrado.");
            }

            var remitenteCorreo = string.IsNullOrWhiteSpace(_opciones.RemitenteCorreo)
                ? _opciones.Usuario
                : _opciones.RemitenteCorreo;

            if (string.IsNullOrWhiteSpace(_opciones.Host) ||
                string.IsNullOrWhiteSpace(remitenteCorreo) ||
                string.IsNullOrWhiteSpace(_opciones.Usuario) ||
                string.IsNullOrWhiteSpace(_opciones.Contrasena))
            {
                return (false, "La configuración SMTP no está completa.");
            }

            try
            {
                using var mensaje = new MailMessage();
                mensaje.From = new MailAddress(remitenteCorreo, string.IsNullOrWhiteSpace(_opciones.RemitenteNombre) ? "Restaurante Escuela INTECAP" : _opciones.RemitenteNombre, Encoding.UTF8);
                mensaje.To.Add(new MailAddress(usuario.email, usuario.nombre, Encoding.UTF8));
                mensaje.Subject = "Restablecimiento de contraseña – Restaurante Escuela INTECAP";
                mensaje.SubjectEncoding = Encoding.UTF8;
                mensaje.BodyEncoding = Encoding.UTF8;
                mensaje.IsBodyHtml = true;
                mensaje.Body = ConstruirCuerpoCorreo(usuario, nuevaPasswordTemporal);

                using var smtp = new SmtpClient(_opciones.Host, _opciones.Puerto)
                {
                    EnableSsl = _opciones.UsarSsl,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false
                };

                if (!string.IsNullOrWhiteSpace(_opciones.Usuario))
                {
                    smtp.Credentials = new NetworkCredential(_opciones.Usuario, _opciones.Contrasena);
                }

                await smtp.SendMailAsync(mensaje);
                return (true, "Correo enviado correctamente.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar el correo de restablecimiento para el usuario {UsuarioId}", usuario.id);
                return (false, "No se pudo enviar el correo de notificación.");
            }
        }

        private static string ConstruirCuerpoCorreo(Usuario usuario, string nuevaPasswordTemporal)
        {
            static string E(string? valor) => System.Net.WebUtility.HtmlEncode(valor ?? string.Empty);

            return $@"<!DOCTYPE html>
<html lang='es'>
<head>
  <meta charset='utf-8' />
  <meta name='viewport' content='width=device-width, initial-scale=1.0' />
</head>
<body style='font-family: Arial, Helvetica, sans-serif; color: #1f2937; line-height: 1.6; background-color: #f8fafc; margin: 0; padding: 0;'>
  <div style='max-width: 640px; margin: 0 auto; padding: 24px;'>
    <div style='background: #ffffff; border: 1px solid #e5e7eb; border-radius: 12px; padding: 28px;'>
      <h2 style='color: #0d6efd; margin-top: 0;'>Restablecimiento de contraseña – Restaurante Escuela INTECAP</h2>
      <p>Estimado/a <strong>{E(usuario.nombre)}</strong>:</p>
      <p>Le informamos que su contraseña de acceso al sistema <strong>Restaurante Escuela INTECAP</strong> ha sido restablecida exitosamente por el administrador.</p>
      <p><strong>Sus nuevas credenciales de acceso son:</strong></p>
      <div style='background: #f8f9fa; border: 1px solid #dee2e6; border-radius: 8px; padding: 16px; margin: 16px 0;'>
        <p style='margin: 0 0 8px 0;'><strong>Usuario:</strong> {E(usuario.nombre)}</p>
        <p style='margin: 0 0 8px 0;'><strong>Correo:</strong> {E(usuario.email)}</p>
        <p style='margin: 0;'><strong>Contraseña temporal:</strong> {E(nuevaPasswordTemporal)}</p>
      </div>
      <p>Le recomendamos mantener estas credenciales de forma segura y, si el sistema dispone de esta funcionalidad, cambiar la contraseña posteriormente.</p>
      <p>Si usted no solicitó este cambio, por favor comuníquese con el administrador del sistema.</p>
      <p>Saludos cordiales,<br/>Administración<br/>Restaurante Escuela INTECAP</p>
    </div>
  </div>
</body>
</html>";
        }
    }
}