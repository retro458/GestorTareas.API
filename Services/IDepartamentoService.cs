using GestorTareas.API.DTOs.Departamento;

namespace GestorTareas.API.Services;
public interface IDepartamentoService
{
    Task<DepartamentoResponseDto> CrearDepartamentoAsync(string nombre, string descripcion);
    Task<IEnumerable<DepartamentoResponseDto>> ObtenerDepartamentosAsync();
    Task<IEnumerable<DepartamentoResponseDto>> ObtenerMisDepartamentosAsync(int usuarioId);
    Task CambiarEstadoActivoAsync(int departamentoId, bool nuevoEstado);
    Task<DepartamentoResponseDto> EditarDepartamentoAsync(int id, string nombre, string? descripcion);
    Task<IEnumerable<DepartamentoResponseDto>> ObtenerDepartamentosInactivosAsync();
}
