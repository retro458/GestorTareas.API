using GestorTareas.API.DTOs.Auth;
using GestorTareas.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace GestorTareas.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IWebHostEnvironment _env;

    public AuthController(IAuthService authService, IWebHostEnvironment env)
    {
        _authService = authService;
        _env = env;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginDto dto)
    {
        var usuario = await _authService.ValidarCredencialesAsync(dto.NombreUsuario, dto.Password);
        if (usuario is null)
            return Unauthorized(new { mensaje = "Usuario o contraseña incorrectos." });

        var token = _authService.GenerarToken(usuario);

        Response.Cookies.Append("X-Access-Token", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = !_env.IsDevelopment(), // true en producción (requiere HTTPS)
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddMinutes(60)
        });

        return Ok(usuario);
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("X-Access-Token");
        return Ok(new { mensaje = "Sesión cerrada." });
    }
}