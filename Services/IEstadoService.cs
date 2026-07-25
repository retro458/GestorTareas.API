using GestorTareas.API.Dtos.Estados;

namespace GestorTareas.API.Services;

public interface IEstadoService
{
    Task<IEnumerable<EstadoResponseDto>> ObtenerEstadosAsync();
}
