using GestorTareas.API.DTOs.Usuarios;
using GestorTareas.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GestorTareas.API.Extensions;
using GestorTareas.API.Data;
using Microsoft.EntityFrameworkCore;
using GestorTareas.API.Models;
using System.Security.Claims;
namespace GestorTareas.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsuarioController : ControllerBase
{
    private readonly IUsuarioService _usuarioService;
    private readonly AppDbContext _context; 
    public UsuarioController(IUsuarioService usuarioService, AppDbContext context)
    {
        _usuarioService = usuarioService;
        _context = context;
    }

    [HttpPost("register")]
    [Authorize(Roles = "Jefe,Encargado Departamento")]
    public async Task<ActionResult<UsuarioResponseDto>> RegisterAsync([FromBody] CrearUsuariosDto request)
    {
        try
        {
            await _usuarioService.RegisterAsync(request.Nombre!, request.NombreUsuario!, request.Password!, request.NombreRol!, request.Departamento!, User.ObtenerRol(), User.ObtenerDepartamentosIds()?.FirstOrDefault());
            return Ok(new { message = "Usuario registrado exitosamente" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    
    }
[HttpGet("empleados/departamento")]
[Authorize(Roles = "Encargado Departamento")]
public async Task<ActionResult<IEnumerable<UsuarioResponseDto>>> ObtenerEmpleadosDeMiDepartamento()
{
    var departamentoIds = User.ObtenerDepartamentosIds();

    if (departamentoIds == null || departamentoIds.Count == 0)
        return BadRequest(new { error = "No se pudo determinar el departamento del usuario actual." });

    var departamentoId = departamentoIds[0];
    var claimUsuarioId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(claimUsuarioId) || !int.TryParse(claimUsuarioId, out int usuarioActualId))
    {
        return Unauthorized(new { error = "No se pudo identificar al usuario actual en la sesión." });
    }
    var empleados = await _usuarioService.GetEmpleadosPorDepartamentoAsync(departamentoId, usuarioActualId);
    return Ok(empleados);
}
[HttpGet("empleados/todos")]
[Authorize(Roles = "Jefe")]
public async Task<ActionResult<IEnumerable<UsuarioResponseDto>>> ObtenerTodosLosEmpleados()
{
    var empleados = await _usuarioService.GetTodosLosEmpleadosAsync();
    return Ok(empleados);
}

[HttpGet("verificar-cuenta")]
public async Task<IActionResult> VerificarCuenta([FromQuery] string token)
{
    var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.TokenVerificacion == token);
    if (usuario == null)
        return BadRequest(new { error = "Token de verificación inválido o ya utilizado." });

    usuario.EmailVerificacion = true;
    usuario.TokenVerificacion = null;
    await _context.SaveChangesAsync();

    return Ok(new { mensaje = "Cuenta verificada correctamente. Ya puedes iniciar sesión." });
}
}