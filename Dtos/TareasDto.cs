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
    public string AsignadoANombre { get; set; } = string.Empty;
    public DateTime? FechaVencimiento { get; set; }
    public DateTime? FechaCreacion { get; set; }
}