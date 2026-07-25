using GestorTareas.API.DTOs.Tareas;
using GestorTareas.API.Models;

namespace GestorTareas.API.Services;
public interface ITareaService
{
    Task<TareaResponseDto> CrearTareaAsync(CrearTareaDto dto, int creadoPorId, string rolCreador, int? departamentoCreadorId);
 
    Task<IEnumerable<TareaResponseDto>> ObtenerTareasAsync(int usuarioActualId, string rolActual, int? departamentoActualId);
 
   Task<TareaResponseDto> CambiarEstadoAsync(int tareaId, int nuevoEstadoId, int usuarioActualId, string rolActual);
 
    Task<TareaResponseDto> ReasignarTareaAsync(int tareaId, int nuevoAsignadoA, int usuarioQueReasignaId, string rolQueReasigna, int? departamentoQueReasignaId);
}
 