using GestorTareas.API.Extensions;
using GestorTareas.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestorTareas.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificacionesController : ControllerBase
{
    private readonly INotificacionService _notificacionService;

    public NotificacionesController(INotificacionService notificacionService)
    {
        _notificacionService = notificacionService;
    }

    [HttpGet("no-leidas")]
    public async Task<ActionResult<IEnumerable<NotificacionResponseDto>>> ObtenerNoLeidas()
    {
        var notificaciones = await _notificacionService.ObtenerNoLeidasAsync(User.ObtenerUsuarioId());
        return Ok(notificaciones);
    }

    [HttpPatch("{id}/marcar-leida")]
    public async Task<IActionResult> MarcarComoLeida(int id)
    {
        try
        {
            await _notificacionService.MarcarComoLeidaAsync(id, User.ObtenerUsuarioId());
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}