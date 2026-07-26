using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace GestorTareas.API.Services;

public interface IEmailService
{
    Task EnviarCorreoVerificacionAsync(string email, string nombre, string token);
    Task EnviarCorreoPruebaAsync(string emailDestino);
}

public class EmailService : IEmailService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _fromEmail;
    private readonly string _frontendUrl;

    public EmailService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _apiKey = DotNetEnv.Env.GetString("RESEND_API_KEY");
        _fromEmail = DotNetEnv.Env.GetString("RESEND_FROM");
        _frontendUrl = DotNetEnv.Env.GetString("FRONTEND_URL");
    }

    public async Task EnviarCorreoVerificacionAsync(string email, string nombre, string token)
    {
        var enlace = $"{_frontendUrl}/verificar-cuenta?token={token}";

        var html = $"<p>Hola {nombre},</p>" +
                   $"<p>Se ha creado una cuenta para ti en GestorTareas. Haz clic en el siguiente enlace para activarla:</p>" +
                   $"<p><a href='{enlace}'>Verificar cuenta</a></p>";

        await EnviarAsync(email, "Verifica tu cuenta - GestorTareas", html);
    }

    public async Task EnviarCorreoPruebaAsync(string emailDestino)
    {
        await EnviarAsync(
            emailDestino,
            "Correo de prueba - GestorTareas",
            "<p>Si estás leyendo esto, la configuración con Resend funciona correctamente.</p>"
        );
    }

    private async Task EnviarAsync(string destino, string asunto, string html)
    {
        var payload = new
        {
            from = _fromEmail,
            to = new[] { destino },
            subject = asunto,
            html
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var contenidoError = await response.Content.ReadAsStringAsync();
            throw new Exception($"Error al enviar correo vía Resend: {response.StatusCode} - {contenidoError}");
        }
    }
}