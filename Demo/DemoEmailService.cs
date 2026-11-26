using GestorTareas.API.Services;

namespace GestorTareas.API.Demo;

// En la demo no se envian correos (ni se necesitan credenciales de Resend):
// se deja constancia en el log, sin el destinatario, que podria ser real
// si alguien lo escribe en la demo.
public sealed class DemoEmailService : IEmailService
{
    private readonly ILogger<DemoEmailService> _logger;

    public DemoEmailService(ILogger<DemoEmailService> logger) => _logger = logger;

    public Task EnviarCorreoVerificacionAsync(string email, string nombre, string token)
    {
        _logger.LogInformation("Demo: correo NO enviado (verificación de cuenta). Destinatario omitido.");
        return Task.CompletedTask;
    }

    public Task EnviarCorreoPruebaAsync(string emailDestino)
    {
        _logger.LogInformation("Demo: correo NO enviado (prueba). Destinatario omitido.");
        return Task.CompletedTask;
    }
}
