using System.Threading.RateLimiting;
using GestorTareas.API.Services;

namespace GestorTareas.API.Demo;

// Todo lo que el modo demo agrega a la app. Program.cs solo llama a estos
// metodos cuando Demo__Activo=true, asi que produccion no cambia.
public static class DemoExtensiones
{
    public const int IntentosLoginPorMinuto = 5;

    public static void AgregarServiciosDemo(this IServiceCollection services)
    {
        // Correo: se registra despues del EmailService real y lo reemplaza.
        services.AddScoped<IEmailService, DemoEmailService>();

        services.Configure<Microsoft.AspNetCore.Mvc.MvcOptions>(o => o.Filters.Add<DemoFiltro>());

        // Rate limiting del login, por IP del visitante. Detras de Cloudflare
        // Tunnel todas las peticiones llegan desde cloudflared, asi que la IP
        // real viene en CF-Connecting-IP.
        services.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            o.OnRejected = (contexto, token) => new ValueTask(contexto.HttpContext.Response.WriteAsJsonAsync(
                new { mensaje = "Demasiados intentos de inicio de sesión. Espera un minuto.", error = "Demasiados intentos de inicio de sesión. Espera un minuto." },
                token));
            o.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(http =>
            {
                var esLogin = HttpMethods.IsPost(http.Request.Method)
                    && http.Request.Path.Equals("/api/auth/login", StringComparison.OrdinalIgnoreCase);
                if (!esLogin)
                    return RateLimitPartition.GetNoLimiter("");

                var ip = http.Request.Headers["CF-Connecting-IP"].FirstOrDefault()
                    ?? http.Connection.RemoteIpAddress?.ToString()
                    ?? "desconocida";
                return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = IntentosLoginPorMinuto,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                });
            });
        });
    }

    public static void UsarDemo(this WebApplication app)
    {
        app.Use(async (http, siguiente) =>
        {
            var h = http.Response.Headers;
            h["X-Robots-Tag"] = "noindex, nofollow"; // que los buscadores no la indexen
            h["X-Content-Type-Options"] = "nosniff";
            h["X-Frame-Options"] = "DENY";
            h["Referrer-Policy"] = "strict-origin-when-cross-origin";
            h["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
            await siguiente();
        });
        app.UseRateLimiter();
    }
}
