using GestorTareas.API.DTOs.ComentariosDto;
using GestorTareas.API.Services;
using Microsoft.AspNetCore.Mvc;
using GestorTareas.API.Data;
using GestorTareas.API.Models;
using Microsoft.EntityFrameworkCore;
using GestorTareas.API.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace GestorTareas.API.Services;

public class ComentarioService : IComentarioService
{
    private readonly AppDbContext _context;
    private readonly IHubContext<TareasHub> _hub;

    public ComentarioService(AppDbContext context, IHubContext<TareasHub> hub)
    {
        _context = context;
        _hub = hub;
    }

    public async Task<ComentarioResponseDto> CrearComentarioAsync(int tareaId, string contenido, int usuarioActualId, string rolActual, List<int> departamentosActualIds)
    {
        var tarea = await _context.Tareas
            .FirstOrDefaultAsync(t => t.Id == tareaId)
            ?? throw new Exception("La tarea no existe.");

        var tareaDepartamentoId = tarea.DepartamentoId
            ?? throw new Exception("La tarea no tiene un departamento asignado.");

        // Verifica acceso segun rol: Empleado solo en las suyas, Encargado en su depto, Jefe sin restriccion
        bool tieneAcceso = rolActual switch
        {
            "Jefe" => true,
            "Encargado Departamento" => departamentosActualIds.Contains(tareaDepartamentoId),
            "Empleado" => tarea.AsignadoA == usuarioActualId,
            _ => false
        };

        if (!tieneAcceso)
            throw new UnauthorizedAccessException("No tiene acceso a comentar en esta tarea.");

        var comentario = new ComentariosTarea
        {
            TareaId = tareaId,
            UsuarioId = usuarioActualId,
            Contenido = contenido,
            FechaCreacion = DateTime.UtcNow
        };

        _context.ComentariosTarea.Add(comentario);
        await _context.SaveChangesAsync();

        var response = await MapearComentarioAsync(comentario.Id);

        await NotificarComentarioAsync(tarea, response, "NuevoComentario");

        return response;
    }

    public async Task<ComentarioResponseDto> EditarComentarioAsync(int comentarioId, string nuevoContenido, string rolActual, List<int> departamentosActualIds)
    {
        var comentario = await _context.ComentariosTarea
            .Include(c => c.Tarea)
            .FirstOrDefaultAsync(c => c.Id == comentarioId)
            ?? throw new Exception("El comentario no existe.");

        if (rolActual != "Jefe" && rolActual != "Encargado Departamento")
            throw new UnauthorizedAccessException("Solo el Jefe o Encargado puede editar comentarios.");

        var tareaDepartamentoId = comentario.Tarea.DepartamentoId
            ?? throw new Exception("La tarea asociada no tiene un departamento asignado.");

        if (rolActual == "Encargado Departamento" && !departamentosActualIds.Contains(tareaDepartamentoId))
            throw new UnauthorizedAccessException("Solo puede editar comentarios de su propio departamento.");

        comentario.Contenido = nuevoContenido;
        comentario.FechaEdicion = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var response = await MapearComentarioAsync(comentario.Id);
        await NotificarComentarioAsync(comentario.Tarea, response, "ComentarioEditado");

        return response;
    }

    public async Task EliminarComentarioAsync(int comentarioId, string rolActual, List<int> departamentosActualIds)
    {
        var comentario = await _context.ComentariosTarea
            .Include(c => c.Tarea)
            .FirstOrDefaultAsync(c => c.Id == comentarioId)
            ?? throw new Exception("El comentario no existe.");

        if (rolActual != "Jefe" && rolActual != "Encargado Departamento")
            throw new UnauthorizedAccessException("Solo el Jefe o Encargado puede eliminar comentarios.");

        var tareaDepartamentoId = comentario.Tarea.DepartamentoId
            ?? throw new Exception("La tarea asociada no tiene un departamento asignado.");

        if (rolActual == "Encargado Departamento" && !departamentosActualIds.Contains(tareaDepartamentoId))
            throw new UnauthorizedAccessException("Solo puede eliminar comentarios de su propio departamento.");

        var tareaId = comentario.TareaId;
        var departamentoId = comentario.Tarea.DepartamentoId;

        _context.ComentariosTarea.Remove(comentario);
        await _context.SaveChangesAsync();

        await _hub.Clients.Group("jefes").SendAsync("ComentarioEliminado", new { comentarioId, tareaId });
        if (departamentoId.HasValue)
        {
            await _hub.Clients.Group($"departamento-{departamentoId.Value}")
                .SendAsync("ComentarioEliminado", new { comentarioId, tareaId });
        }
    }

    public async Task<IEnumerable<ComentarioResponseDto>> ObtenerComentariosPorTareaAsync(int tareaId)
    {
        var comentarios = await _context.ComentariosTarea
            .Include(c => c.Usuario)
            .Where(c => c.TareaId == tareaId)
            .OrderBy(c => c.FechaCreacion)
            .ToListAsync();

        return comentarios.Select(c => new ComentarioResponseDto
        {
            Id = c.Id,
            TareaId = c.TareaId,
            Contenido = c.Contenido,
            UsuarioId = c.UsuarioId,
            UsuarioNombre = c.Usuario?.Nombre ?? "Usuario desconocido",
            FechaCreacion = c.FechaCreacion,
            FechaEdicion = c.FechaEdicion
        });
    }

    private async Task<ComentarioResponseDto> MapearComentarioAsync(int comentarioId)
    {
        var c = await _context.ComentariosTarea
            .Include(x => x.Usuario)
            .FirstAsync(x => x.Id == comentarioId);

        return new ComentarioResponseDto
        {
            Id = c.Id,
            TareaId = c.TareaId,
            Contenido = c.Contenido,
            UsuarioId = c.UsuarioId,
            UsuarioNombre = c.Usuario?.Nombre ?? "Usuario desconocido",
            FechaCreacion = c.FechaCreacion,
            FechaEdicion = c.FechaEdicion
        };
    }

    private async Task NotificarComentarioAsync(Tarea tarea, ComentarioResponseDto comentario, string nombreEvento)
    {
        await _hub.Clients.Group("jefes").SendAsync(nombreEvento, comentario);

        if (tarea.DepartamentoId.HasValue)
        {
            await _hub.Clients.Group($"departamento-{tarea.DepartamentoId.Value}")
                .SendAsync(nombreEvento, comentario);
        }

        // Tambien al empleado asignado, si no es quien esta comentando
        if (tarea.AsignadoA != comentario.UsuarioId)
        {
            await _hub.Clients.User(tarea.AsignadoA.ToString())
                .SendAsync(nombreEvento, comentario);
        }
    }
}

