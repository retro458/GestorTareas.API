using GestorTareas.API.Dtos.Estados;
using GestorTareas.API.Data;
using Microsoft.EntityFrameworkCore;

namespace GestorTareas.API.Services;
public class EstadoService : IEstadoService
{
    private readonly AppDbContext _context;
    public EstadoService(AppDbContext context)
    {
        _context = context;
    }
    public async Task<IEnumerable<EstadoResponseDto>> ObtenerEstadosAsync()
    {
       return await _context.Estados
            .Where(e => e.Activo)
            .OrderBy(e => e.Orden)
            .Select(e => new EstadoResponseDto
            {
                Id = e.EstadoId,
                Nombre = e.NombreEstado
            })
            .ToListAsync();
    
    }
}