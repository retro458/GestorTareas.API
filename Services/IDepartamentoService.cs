using GestorTareas.API.DTOs.Departamento;

namespace GestorTareas.API.Services;
public interface IDepartamentoService
{
    Task<DepartamentoResponseDto> CrearDepartamentoAsync(string nombre, string descripcion);
    Task<IEnumerable<DepartamentoResponseDto>> ObtenerDepartamentosAsync();
}