namespace GestorTareas.API.DTOs.ComentariosDto;

public class CrearComentarioDto
{
    public string Contenido { get; set; } = string.Empty;
}

public class EditarComentarioDto
{
    public string Contenido { get; set; } = string.Empty;
}

public class ComentarioResponseDto
{
    public int Id { get; set; }
    public int TareaId { get; set; }
    public string Contenido { get; set; } = string.Empty;
    public int UsuarioId { get; set; }
    public string UsuarioNombre { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaEdicion { get; set; }
}
