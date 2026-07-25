using Microsoft.EntityFrameworkCore;
using GestorTareas.API.Data;
namespace GestorTareas.API.Services;

public class NotificacionResponseDto
{
    public int Id { get; set; }
    public int? TareaId { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public bool Leida { get; set; }
    public DateTime FechaCreacion { get; set; }
}

public interface INotificacionService
{
    Task<IEnumerable<NotificacionResponseDto>> ObtenerNoLeidasAsync(int usuarioId);
    Task MarcarComoLeidaAsync(int notificacionId, int usuarioId);
}

public class NotificacionService : INotificacionService
{
    private readonly AppDbContext _context;

    public NotificacionService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<NotificacionResponseDto>> ObtenerNoLeidasAsync(int usuarioId)
    {
        return await _context.Notificaciones
            .Where(n => n.UsuarioId == usuarioId && !n.Leida)
            .OrderByDescending(n => n.FechaCreacion)
            .Select(n => new NotificacionResponseDto
            {
                Id = n.Id,
                TareaId = n.TareaId,
                Mensaje = n.Mensaje,
                Leida = n.Leida,
                FechaCreacion = n.FechaCreacion
            })
            .ToListAsync();
    }

    public async Task MarcarComoLeidaAsync(int notificacionId, int usuarioId)
    {
        var notificacion = await _context.Notificaciones
            .FirstOrDefaultAsync(n => n.Id == notificacionId && n.UsuarioId == usuarioId)
            ?? throw new Exception("Notificación no encontrada.");

        notificacion.Leida = true;
        await _context.SaveChangesAsync();
    }
}