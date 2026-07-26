using GestorTareas.API.DTOs.Usuarios;
using GestorTareas.API.Models;
using GestorTareas.API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GestorTareas.API.Data;

namespace GestorTareas.API.Services;
public class UsuarioService : IUsuarioService 
{
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;
    public UsuarioService(AppDbContext context, IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

   public async Task<UsuarioResponseDto> RegisterAsync(
    string nombre, string email, string password, string nombreRol, string departamento,
    string rolCreador, int? departamentoCreadorId)
{
    var usuarioExistente = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == email);
    if (usuarioExistente != null)
        throw new Exception("El usuario ya existe.");

    var rol = await _context.Roles.FirstOrDefaultAsync(r => r.NombreRol == nombreRol);
    if (rol == null)
        throw new Exception("El rol especificado no existe.");

    if (rol.NombreRol == "Jefe")
    {
        var jefeExistente = await _context.Usuarios.FirstOrDefaultAsync(u => u.RolId == rol.RolId);
        if (jefeExistente != null)
            throw new Exception("Ya hay un jefe registrado, no se puede registrar otro jefe.");
    }

    // NUEVO: un Encargado solo puede estar creando usuarios de su propio rol/alcance
    if (rolCreador != "Jefe" && rolCreador != "Encargado Departamento")
        throw new UnauthorizedAccessException("No tiene permisos para crear usuarios.");

    var departamentoExistente = await _context.Departamentos.FirstOrDefaultAsync(d => d.Nombre == departamento);
    if (departamentoExistente == null)
        throw new Exception("El departamento especificado no existe.");

    // NUEVO: el Encargado solo puede crear usuarios en su propio departamento,
    // sin importar que departamento haya intentado mandar el frontend.
    if (rolCreador == "Encargado Departamento" && departamentoExistente.Id != departamentoCreadorId)
        throw new UnauthorizedAccessException("Solo puede crear usuarios dentro de su propio departamento.");
        
    var tokenVerificacion = Guid.NewGuid().ToString(); // Genera un token de verificación único
    var nuevoUsuario = new Usuario
    {
        Nombre = nombre,
        Email = email,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
        RolId = rol.RolId,
        DepartamentoId = departamentoExistente.Id,
        Activo = true,
        EmailVerificacion = false,
        TokenVerificacion = tokenVerificacion
    };

    _context.Usuarios.Add(nuevoUsuario);
    await _context.SaveChangesAsync();
    await _emailService.EnviarCorreoVerificacionAsync(nuevoUsuario.Email, nuevoUsuario.Nombre, tokenVerificacion);
    return new UsuarioResponseDto
    {
        Email = nuevoUsuario.Email,
        NombreRol = rol.NombreRol,
        Departamento = departamentoExistente.Nombre,
    };
}
    public async Task<UsuarioResponseDto> GetUsuarioByIdAsync(int id)
    {
        var usuario = await _context.Usuarios
            .Include(u => u.Rol)
            .Include(u => u.Departamento)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (usuario == null)
        {
            throw new Exception("Usuario no encontrado.");
        }

        return new UsuarioResponseDto
        {
            Id = usuario.Id,
            Nombre = usuario.Nombre,
            Email = usuario.Email,
            NombreRol = usuario.Rol.NombreRol,
            Departamento = usuario.Departamento.Nombre,
        };
    }

   
public async Task<IEnumerable<UsuarioResponseDto>> GetEmpleadosPorDepartamentoAsync(int departamentoId)
{
    // Solo empleados (no jefes ni encargados) del departamento especifico.
    // Un Encargado no deberia poder "reasignar" una tarea a otro Encargado
    // o al Jefe - solo a los empleados que el mismo supervisa.
    var empleados = await _context.Usuarios
        .Include(u => u.Rol)
        .Include(u => u.Departamento)
        .Where(u => u.DepartamentoId == departamentoId && u.Rol.NombreRol == "Empleado" && u.Activo == true)
        .OrderBy(u => u.Nombre)
        .ToListAsync();
 
    return empleados.Select(u => new UsuarioResponseDto
    {
        Id = u.Id,
        Nombre = u.Nombre,
        Email = u.Email,
        NombreRol = u.Rol.NombreRol,
        Departamento = u.Departamento?.Nombre ?? "Sin asignar"
    });
}
 
public async Task<IEnumerable<UsuarioResponseDto>> GetTodosLosEmpleadosAsync()
{
    // El Jefe puede asignar/reasignar a cualquier empleado de cualquier
    // departamento, asi que aqui no filtramos por DepartamentoId.
    var empleados = await _context.Usuarios
        .Include(u => u.Rol)
        .Include(u => u.Departamento)
        .Where(u => u.Rol.NombreRol == "Empleado" && u.Activo == true)
        .OrderBy(u => u.Departamento!.Nombre).ThenBy(u => u.Nombre)
        .ToListAsync();
 
    return empleados.Select(u => new UsuarioResponseDto
    {
        Id = u.Id,
        Nombre = u.Nombre,
        Email = u.Email,
        NombreRol = u.Rol.NombreRol,
        Departamento = u.Departamento?.Nombre ?? "Sin asignar"
    });
   } 
}