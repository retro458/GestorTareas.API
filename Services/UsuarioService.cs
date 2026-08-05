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
    string nombre, string nombreUsuario, string password, string nombreRol, List<int> departamentosIds,
    string rolCreador, List<int> departamentosCreadorIds)
{
    var usuarioExistente = await _context.Usuarios.FirstOrDefaultAsync(u => u.NombreUsuario == nombreUsuario);
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

    if (rolCreador != "Jefe" && rolCreador != "Encargado Departamento")
        throw new UnauthorizedAccessException("No tiene permisos para crear usuarios.");

    if (!departamentosIds.Any())
        throw new Exception("El usuario debe pertenecer al menos a un departamento.");

    // Verifica que todos los departamentos solicitados existan realmente
    var departamentosValidos = await _context.Departamentos
        .Where(d => departamentosIds.Contains(d.Id))
        .Select(d => d.Id)
        .ToListAsync();

    if (departamentosValidos.Count != departamentosIds.Distinct().Count())
        throw new Exception("Uno o más departamentos especificados no existen.");

    // El Encargado solo puede asignar departamentos a los que el mismo pertenece
    if (rolCreador == "Encargado Departamento" && departamentosIds.Any(d => !departamentosCreadorIds.Contains(d)))
        throw new UnauthorizedAccessException("Solo puede asignar departamentos a los que usted mismo pertenece.");

    var nuevoUsuario = new Usuario
    {
        Nombre = nombre,
        NombreUsuario = nombreUsuario,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
        RolId = rol.RolId,
        Activo = true
    };

    _context.Usuarios.Add(nuevoUsuario);
    await _context.SaveChangesAsync(); // necesitamos el Id generado antes de poblar la tabla intermedia

    foreach (var deptoId in departamentosIds)
    {
        _context.UsuariosDepartamentos.Add(new UsuariosDepartamentos
        {
            UsuarioId = nuevoUsuario.Id,
            DepartamentoId = deptoId
        });
    }
    await _context.SaveChangesAsync();

    var nombresDepartamentos = await _context.Departamentos
        .Where(d => departamentosIds.Contains(d.Id))
        .Select(d => d.Nombre)
        .ToListAsync();

    var departamentosInfo = await _context.Departamentos
    .Where(d => departamentosIds.Contains(d.Id))
    .Select(d => new DepartamentoResumenDto { Id = d.Id, Nombre = d.Nombre })
    .ToListAsync();
return new UsuarioResponseDto
{
    Id = nuevoUsuario.Id,
    Nombre = nuevoUsuario.Nombre,
    NombreUsuario = nuevoUsuario.NombreUsuario,
    NombreRol = rol.NombreRol,
    Departamentos = departamentosInfo
};
}
    public async Task<UsuarioResponseDto> GetUsuarioByIdAsync(int id)
    {
        var usuario = await _context.Usuarios
            .Include(u => u.Rol)
            .Include(u => u.UsuariosDepartamentos)
                .ThenInclude(ud => ud.Departamento)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (usuario == null)
        {
            throw new Exception("Usuario no encontrado.");
        }

        return new UsuarioResponseDto
        {
            Id = usuario.Id,
            Nombre = usuario.Nombre,
            NombreUsuario = usuario.NombreUsuario,
            NombreRol = usuario.Rol?.NombreRol,
            Departamentos = usuario.UsuariosDepartamentos.Select(ud => new DepartamentoResumenDto
            {
                Id = ud.DepartamentoId,
                Nombre = ud.Departamento!.Nombre
            }).ToList()
        };
    }

   
public async Task<IEnumerable<UsuarioResponseDto>> GetEmpleadosPorDepartamentoAsync(List<int> departamentosIds,int usuarioActualId)
{
    // Solo empleados (no jefes ni encargados) del departamento especifico.
    // Un Encargado no deberia poder "reasignar" una tarea a otro Encargado
    // o al Jefe - solo a los empleados que el mismo supervisa.
     var empleados = await _context.Usuarios
        .Include(u => u.Rol)
        .Include(u => u.UsuariosDepartamentos)
            .ThenInclude(ud => ud.Departamento)
        .Where(u => u.UsuariosDepartamentos.Any(ud => departamentosIds.Contains(ud.DepartamentoId))
                 && u.Activo == true
                 && (u.Rol!.NombreRol == "Empleado" || u.Id == usuarioActualId)) // incluye al propio Encargado
        .OrderBy(u => u.Nombre)
        .ToListAsync();
 
    return empleados.Select(u => new UsuarioResponseDto
    {
        Id = u.Id,
        Nombre = u.Nombre,
        NombreUsuario = u.NombreUsuario,
        NombreRol = u.Rol!.NombreRol,
        Departamentos = u.UsuariosDepartamentos
                .Select(ud => new DepartamentoResumenDto { Id = ud.DepartamentoId, Nombre = ud.Departamento!.Nombre })
                .ToList()
    });
}
 
public async Task<IEnumerable<UsuarioResponseDto>> GetTodosLosEmpleadosAsync()
{
    // El Jefe puede asignar/reasignar a cualquier empleado de cualquier
    // departamento, asi que aqui no filtramos por DepartamentoId.
    var empleados = await _context.Usuarios
        .Include(u => u.Rol)
        .Include(u => u.UsuariosDepartamentos)
            .ThenInclude(ud => ud.Departamento)
        .Where(u => u.Rol!.NombreRol == "Empleado" && u.Activo == true)
        .OrderBy(u => u.UsuariosDepartamentos.First().Departamento!.Nombre).ThenBy(u => u.Nombre)
        .ToListAsync();
 
    return empleados.Select(u => new UsuarioResponseDto
    {
        Id = u.Id,
        Nombre = u.Nombre,
        NombreUsuario = u.NombreUsuario,
        NombreRol = u.Rol!.NombreRol,
        Departamentos = u.UsuariosDepartamentos
                .Select(ud => new DepartamentoResumenDto { Id = ud.DepartamentoId, Nombre = ud.Departamento!.Nombre })
                .ToList()
    });
   } 
}
