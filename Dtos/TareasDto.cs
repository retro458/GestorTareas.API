namespace GestorTareas.API.DTOs.Tareas;

public class CrearTareaDto
{
    public string Titulo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int AsignadoA { get; set; }
    public int DepartamentoId {get;set;}
    public int PrioridadId { get; set; }
    public DateTime? FechaVencimiento { get; set; }
}

public class EditarTareaDto
{
    public string? Titulo {get;set; }
    public string? Descripcion {get;set; }
    public int? PrioridadId {get;set; }
    public DateTime? FechaVencimiento {get;set; }
}

public class ActualizarEstadoTareaDto
{
    public int EstadoId { get; set; }
}

public class ReasignarTareaDto
{
    public int NuevoAsignadoA { get; set; }
}

public class TareaResponseDto
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string Prioridad { get; set; } = string.Empty;
    public int? AsignadoA { get; set; }
    public int? DepartamentoId {get;set;}
    public string AsignadoANombre { get; set; } = string.Empty;
    public DateTime? FechaVencimiento { get; set; }
    public DateTime? FechaCreacion { get; set; }
    public int? DiaAtraso =>
        (FechaVencimiento.HasValue && Estado !="Completa" && Estado !="Cancelada" && FechaVencimiento.Value < DateTime.UtcNow)
           ? (int)(DateTime.UtcNow - FechaVencimiento.Value).TotalDays
           : null;
}


public class HistorialTareaResponseDto
{
    public int Id { get; set; }
    public string Accion { get; set; } = string.Empty;
    public string UsuarioNombre { get; set; } = string.Empty;
    public DateTime? Fecha { get; set; }
}

public class TareaDetalleResponseDto
{
    public TareaResponseDto Tarea { get; set; } = null!;
    public List<HistorialTareaResponseDto> Historial { get; set; } = new();
}
