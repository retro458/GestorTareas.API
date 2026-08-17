using GestorTareas.API.DTOs.ComentariosDto;

namespace GestorTareas.API.Services;
public interface IComentarioService
{
    Task<ComentarioResponseDto> CrearComentarioAsync(int tareaId, string contenido, int usuarioActualId, string rolActual, List<int> departamentosActualIds);
    Task<ComentarioResponseDto> EditarComentarioAsync(int comentarioId, string nuevoContenido, string rolActual, List<int> departamentosActualIds);
    Task EliminarComentarioAsync(int comentarioId, string rolActual, List<int> departamentosActualIds);
    Task<IEnumerable<ComentarioResponseDto>> ObtenerComentariosPorTareaAsync(int tareaId);

    
    

}
