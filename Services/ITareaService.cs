using GestorTareas.API.DTOs.Tareas;
using GestorTareas.API.Models;

namespace GestorTareas.API.Services;
public interface ITareaService
{

    Task<TareaResponseDto> CrearTareaAsync(CrearTareaDto dto, int creadoPorId, string rolCreador, List<int> departamentosCreadorIds);
    Task<IEnumerable<TareaResponseDto>> ObtenerTareasAsync(int usuarioActualId, string rolActual, List<int> departamentosActualIds);
// Y lo mismo para ReasignarTareaAsync si tambien recibe departamento
   Task<TareaResponseDto> CambiarEstadoAsync(int tareaId, int nuevoEstadoId, int usuarioActualId, string rolActual);
 
    // ITareaService.cs
 Task<TareaResponseDto> ReasignarTareaAsync(int tareaId, int nuevoAsignadoA, int usuarioQueReasignaId, string rolQueReasigna, List<int> departamentosQueReasignaIds);
}
 