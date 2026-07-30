using GestorTareas.API.DTOs.Tareas;
using Microsoft.EntityFrameworkCore;
using GestorTareas.API.Data;
using GestorTareas.API.Models;
using GestorTareas.API.Hubs;
using Microsoft.AspNetCore.SignalR;
namespace GestorTareas.API.Services;

public class TareaService : ITareaService
{
    private readonly AppDbContext _context;
    private readonly IHubContext<TareasHub> _hub;
    
    private const string ESTADO_COMPLETADA = "Completada";
    private const string ESTADO_CANCELADA = "Cancelada";

    public TareaService(AppDbContext context, IHubContext<TareasHub> hub)
    {
        _context = context;
        _hub = hub;
    }

 
    public async Task<TareaResponseDto> CrearTareaAsync(CrearTareaDto dto, int creadoPorId, string rolCreador, List<int> departamentosCreadorIds)
    {
        // El Encargado solo puede crear tareas en departamentos donde el tenga acceso
        if (rolCreador == "Encargado Departamento" && !departamentosCreadorIds.Contains(dto.DepartamentoId))
    throw new UnauthorizedAccessException("Solo puede crear tareas en departamentos a su cargo.");

 var empleado = await _context.Usuarios
    .Include(u => u.UsuariosDepartamentos)
    .FirstOrDefaultAsync(u => u.Id == dto.AsignadoA)
    ?? throw new Exception("El empleado a asignar no existe.");

    // El empleado destino debe pertenecer al departamento elegido para la tarea
    var empleadoDeptosIds = empleado.UsuariosDepartamentos.Select(d => d.DepartamentoId);
if (!empleadoDeptosIds.Contains(dto.DepartamentoId))
    throw new Exception("El empleado seleccionado no pertenece al departamento elegido para esta tarea.");
        var existeTareaActiva = await _context.Tareas
            .Include(t => t.Estado)
            .AnyAsync(t => t.Titulo == dto.Titulo
                        && t.AsignadoA == dto.AsignadoA
                        && t.Estado.NombreEstado != ESTADO_COMPLETADA
                        && t.Estado.NombreEstado != ESTADO_CANCELADA);
 
        if (existeTareaActiva)
            throw new Exception($"Ya existe una tarea activa con el título '{dto.Titulo}' asignada a este empleado.");
 
        var estadoInicial = await _context.Estados.FirstAsync(e => e.EsEstadoInicial);
 
        var prioridad = await _context.Prioridads.FindAsync(dto.PrioridadId)
            ?? throw new Exception("La prioridad especificada no existe.");
 
        var tarea = new Tarea
        {
            Titulo = dto.Titulo,
            Descripcion = dto.Descripcion,
            EstadoId = estadoInicial.EstadoId,
            PrioridadId = dto.PrioridadId,
            DepartamentoId = empleado.DepartamentoId,
            AsignadoA = dto.AsignadoA,
            CreadoPor = creadoPorId,
            FechaCreacion = DateTime.UtcNow,
            FechaVencimiento = dto.FechaVencimiento
        };
 
        _context.Tareas.Add(tarea);
        await _context.SaveChangesAsync();
 
        _context.HistorialTareas.Add(new HistorialTarea
        {
            TareaId = tarea.Id,
            UsuarioId = creadoPorId,
            Accion = $"Tarea creada y asignada a {empleado.Nombre}.",
            Fecha = DateTime.UtcNow
        });
 
        var notificacion = new Notificaciones
        {
            UsuarioId = dto.AsignadoA,
            TareaId = tarea.Id,
            Mensaje = $"Se te asignó una nueva tarea: '{tarea.Titulo}'.",
            Leida = false,
            FechaCreacion = DateTime.UtcNow
        };
        _context.Notificaciones.Add(notificacion);
 
        await _context.SaveChangesAsync();
 
        var tareaResponse = await MapearTareaResponseAsync(tarea.Id);
 
        // --- Eventos en tiempo real ---
        await _hub.Clients.User(dto.AsignadoA.ToString())
            .SendAsync("NuevaNotificacion", MapearNotificacionResponse(notificacion));
 
        await NotificarActualizacionTareaAsync(tareaResponse, empleado.DepartamentoId);
 
        return tareaResponse;
    }
 
    public async Task<IEnumerable<TareaResponseDto>> ObtenerTareasAsync(int usuarioActualId, string rolActual, List<int> departamentosActualIds)
    {
        IQueryable<Tarea> query = _context.Tareas
            .Include(t => t.Estado)
            .Include(t => t.Prioridad)
            .Include(t => t.AsignadoANavigation);
 
        query = rolActual switch
        {
            "Jefe" => query,
            "Encargado Departamento" => departamentosActualIds != null && departamentosActualIds.Any()
                ? query.Where(t => t.DepartamentoId.HasValue && departamentosActualIds.Contains(t.DepartamentoId.Value))
                : query.Where(t => false),
            "Empleado" => query.Where(t => t.AsignadoA == usuarioActualId),
            _ => query.Where(t => false)
        };
 
        var tareas = await query.OrderByDescending(t => t.FechaCreacion).ToListAsync();
 
        return tareas.Select(MapearTareaResponse);
    }
 
   public async Task<TareaResponseDto> CambiarEstadoAsync(int tareaId, int nuevoEstadoId, int usuarioActualId, string rolActual)
    {
       var tarea = await _context.Tareas.FindAsync(tareaId)
        ?? throw new Exception("La tarea no existe.");

     var nuevoEstado = await _context.Estados.FindAsync(nuevoEstadoId)
        ?? throw new Exception("El estado especificado no existe.");

     bool esCancelacion = nuevoEstado.NombreEstado == "Cancelada";

      if (esCancelacion)
     {
        // Solo Jefe o Encargado pueden cancelar
        if (rolActual != "Jefe" && rolActual != "Encargado Departamento")
            throw new UnauthorizedAccessException("Solo un Jefe o Encargado puede cancelar una tarea.");
        }
        else
        {
        // Cualquier otro cambio de estado sigue siendo exclusivo del empleado asignado
        if (tarea.AsignadoA != usuarioActualId)
            throw new UnauthorizedAccessException("Solo el empleado asignado puede cambiar el estado de esta tarea.");
        }
        var estadoAnterior = await _context.Estados.FindAsync(tarea.EstadoId);
 
        tarea.EstadoId = nuevoEstadoId;
        await _context.SaveChangesAsync();
 
        _context.HistorialTareas.Add(new HistorialTarea
        {
            TareaId = tarea.Id,
            UsuarioId = usuarioActualId,
            Accion = $"Estado cambiado de '{estadoAnterior?.NombreEstado}' a '{nuevoEstado.NombreEstado}'.",
            Fecha = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
 
        var tareaResponse = await MapearTareaResponseAsync(tarea.Id);
 
        // --- Evento en tiempo real: el jefe/encargado ve el cambio sin recargar ---
        await NotificarActualizacionTareaAsync(tareaResponse, tarea.DepartamentoId);
 
        return tareaResponse;
    }
 
    public async Task<TareaResponseDto> ReasignarTareaAsync(int tareaId, int nuevoAsignadoA, int usuarioQueReasignaId, string rolQueReasigna, List<int> departamentosQueReasignaIds)
    {
       
           if (rolQueReasigna != "Jefe" && rolQueReasigna != "Encargado Departamento")
        throw new UnauthorizedAccessException("No tiene permisos para reasignar tareas.");

    var tarea = await _context.Tareas
        .Include(t => t.Estado)
        .FirstOrDefaultAsync(t => t.Id == tareaId)
        ?? throw new Exception("La tarea no existe.");

    if (tarea.Estado.NombreEstado == ESTADO_COMPLETADA || tarea.Estado.NombreEstado == ESTADO_CANCELADA)
        throw new Exception($"No se puede reasignar una tarea en estado '{tarea.Estado.NombreEstado}'.");

    // El Encargado solo puede reasignar tareas de departamentos donde tenga acceso
    if (rolQueReasigna == "Encargado Departamento" && !departamentosQueReasignaIds.Contains(tarea.DepartamentoId))
        throw new UnauthorizedAccessException("Solo puede reasignar tareas de departamentos a su cargo.");

    var nuevoEmpleado = await _context.Usuarios
        .Include(u => u.UsuariosDepartamentos)
        .FirstOrDefaultAsync(u => u.Id == nuevoAsignadoA)
        ?? throw new Exception("El nuevo empleado a asignar no existe.");

    // El nuevo empleado debe pertenecer al MISMO departamento que la tarea
    var nuevoEmpleadoDeptosIds = nuevoEmpleado.UsuariosDepartamentos.Select(ud => ud.DepartamentoId);
    if (!nuevoEmpleadoDeptosIds.Contains(tarea.DepartamentoId))
        throw new Exception("No se puede reasignar la tarea a un empleado que no pertenece a ese departamento.");

    var empleadoAnteriorId = tarea.AsignadoA;
    var empleadoAnterior = await _context.Usuarios.FindAsync(empleadoAnteriorId);

    tarea.AsignadoA = nuevoAsignadoA;
    await _context.SaveChangesAsync();
 
        _context.HistorialTareas.Add(new HistorialTarea
        {
            TareaId = tarea.Id,
            UsuarioId = usuarioQueReasignaId,
            Accion = $"Tarea reasignada de {empleadoAnterior?.Nombre} a {nuevoEmpleado.Nombre}.",
            Fecha = DateTime.UtcNow
        });
 
        var notificacion = new Notificaciones
        {
            UsuarioId = empleadoAnteriorId,
            TareaId = tarea.Id,
            Mensaje = $"Tu tarea '{tarea.Titulo}' fue reasignada a {nuevoEmpleado.Nombre}.",
            Leida = false,
            FechaCreacion = DateTime.UtcNow
        };
        _context.Notificaciones.Add(notificacion);
 
        await _context.SaveChangesAsync();
 
        var tareaResponse = await MapearTareaResponseAsync(tarea.Id);
 
        // --- Eventos en tiempo real ---
        await _hub.Clients.User(empleadoAnteriorId.ToString()!)
            .SendAsync("NuevaNotificacion", MapearNotificacionResponse(notificacion));
 
        await NotificarActualizacionTareaAsync(tareaResponse, tarea.DepartamentoId);
 
        return tareaResponse;
    }
 
    /// Envia el evento "TareaActualizada" al grupo de Jefes (siempre) y al
    /// grupo del departamento correspondiente (si aplica), para que el
    /// dashboard se refresque sin necesidad de recargar la pagina.
    private async Task NotificarActualizacionTareaAsync(TareaResponseDto tarea, int? departamentoId)
    {
        await _hub.Clients.Group("jefes").SendAsync("TareaActualizada", tarea);
 
        if (departamentoId.HasValue)
        {
            await _hub.Clients.Group($"departamento-{departamentoId.Value}")
                .SendAsync("TareaActualizada", tarea);
        }
    }
 
    private static NotificacionResponseDto MapearNotificacionResponse(Notificaciones n)
    {
        return new NotificacionResponseDto
        {
            Id = n.Id,
            TareaId = n.TareaId,
            Mensaje = n.Mensaje,
            Leida = n.Leida,
            FechaCreacion = n.FechaCreacion
        };
    }
 
    private async Task<TareaResponseDto> MapearTareaResponseAsync(int tareaId)
    {
        var tarea = await _context.Tareas
            .Include(t => t.Estado)
            .Include(t => t.Prioridad)
            .Include(t => t.AsignadoANavigation)
            .FirstAsync(t => t.Id == tareaId);
 
        return MapearTareaResponse(tarea);
    }
 
    private static TareaResponseDto MapearTareaResponse(Tarea tarea)
    {
        return new TareaResponseDto
        {
            Id = tarea.Id,
            Titulo = tarea.Titulo,
            Descripcion = tarea.Descripcion,
            Estado = tarea.Estado.NombreEstado,
            Prioridad = tarea.Prioridad.NombrePrioridad,
            AsignadoA = tarea.AsignadoA,
            AsignadoANombre = tarea.AsignadoANavigation!.Nombre,
            FechaVencimiento = tarea.FechaVencimiento,
            FechaCreacion = tarea.FechaCreacion
        };
    }
}