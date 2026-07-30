using GestorTareas.API.DTOs.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GestorTareas.API.Data;

namespace GestorTareas.API.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly string _jwtSecret;
    private readonly string _jwtIssuer;
    private readonly string _jwtAudience;
    private readonly int _jwtExpirationMinutes;

    public AuthService(AppDbContext db, IConfiguration _)
    {
        _db = db;

        // via Env para mantener consistencia con el resto del proyecto.
        _jwtSecret = DotNetEnv.Env.GetString("JWT_SECRET_KEY");
        _jwtIssuer = DotNetEnv.Env.GetString("JWT_ISSUER");
        _jwtAudience = DotNetEnv.Env.GetString("JWT_AUDIENCE");
        _jwtExpirationMinutes = int.TryParse(DotNetEnv.Env.GetString("JWT_EXPIRATION_MINUTES"), out var m) ? m : 60;
    }

    public async Task<LoginResponseDto?> ValidarCredencialesAsync(string nombreUsuario, string password)
    {
        var usuario = await _db.Usuarios
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.NombreUsuario == nombreUsuario && u.Activo == true);

        if (usuario is null)
            return null;

        
        bool passwordValido = BCrypt.Net.BCrypt.Verify(password, usuario.PasswordHash);
        if (!passwordValido)
            return null;

        return new LoginResponseDto
        {
            Id = usuario.Id,
            Nombre = usuario.Nombre,
            Rol = usuario.Rol!.NombreRol,
            DepartamentoId = usuario.DepartamentoId
        };
    }

    public string GenerarToken(LoginResponseDto usuario)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Name, usuario.Nombre),
            new("NombreUsuario", usuario.NombreUsuario),
            new(ClaimTypes.Role, usuario.Rol),
        };

        foreach (var deptoId in usuario.DepartamentosIds!)
        {
            claims.Add(new Claim("DepartamentoId", deptoId.ToString()));
        }
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtIssuer,
            audience: _jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtExpirationMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}