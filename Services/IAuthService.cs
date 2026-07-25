using GestorTareas.API.DTOs.Auth;

namespace GestorTareas.API.Services;

public interface IAuthService
{
    /// Valida credenciales. Devuelve el DTO de usuario si son correctas, null si no.
    Task<LoginResponseDto?> ValidarCredencialesAsync(string email, string password);

    /// Genera el JWT firmado para el usuario dado.
    string GenerarToken(LoginResponseDto usuario);
}