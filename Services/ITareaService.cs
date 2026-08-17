using GestorTareas.API.DTOs.Tareas;
using GestorTareas.API.Models;

namespace GestorTareas.API.Services;
public interface ITareaService
{

    Task<TareaResponseDto> CrearTareaAsync(CrearTareaDto dto, int creadoPorId, string rolCreador, List<int> departamentosCreadorIds);
    Task<IEnumerable<TareaResponseDto>> ObtenerTareasAsync(int usuarioActualId, string rolActual, List<int> departamentosActualIds);
    Task<TareaResponseDto> EditarTareaAsync(int tareaId, EditarTareaDto dto, int usuarioActualId, string rolActual, List<int> departamentosActualIds);
    Task<TareaResponseDto> CambiarEstadoAsync(int tareaId, int nuevoEstadoId, int usuarioActualId, string rolActual);
    Task<IEnumerable<TareaResponseDto>> ObtenerTareasCompletadasAsync(int usuarioActualId, string rolActual, List<int> departamentosActualIds);
    Task<IEnumerable<TareaResponseDto>> ObtenerTareasPorDepartamentoAsync(int departamentoId);
    Task<IEnumerable<HistorialTareaResponseDto>> ObtenerHistorialAsync(int tareaId);
    Task<TareaResponseDto> ReasignarTareaAsync(int tareaId, int nuevoAsignadoA, int usuarioQueReasignaId, string rolQueReasigna, List<int> departamentosQueReasignaIds);
}
 
