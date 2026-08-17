using GestorTareas.API.DTOs.ComentariosDto;
using GestorTareas.API.Services;
using GestorTareas.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GestorTareas.API.Extensions;

namespace GestorTareas.API.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class ComentariosController : ControllerBase
{
    private readonly IComentarioService _comentarioService;

    public ComentariosController(IComentarioService comentarioService)
    {
        _comentarioService = comentarioService;
    }

    [HttpPost("tareas/{tareaId}/comentarios")]
    public async Task<ActionResult<ComentarioResponseDto>> CrearComentario(int tareaId, [FromBody] CrearComentarioDto dto)
    {
        try
        {
            var comentario = await _comentarioService.CrearComentarioAsync(
                tareaId,
                dto.Contenido,
                User.ObtenerUsuarioId(),
                User.ObtenerRol(),
                User.ObtenerDepartamentosIds());

            return Ok(comentario);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("tareas/{tareaId}/comentarios")]
    public async Task<ActionResult<IEnumerable<ComentarioResponseDto>>> ObtenerComentarios(int tareaId)
    {
        var comentarios = await _comentarioService.ObtenerComentariosPorTareaAsync(tareaId);
        return Ok(comentarios);
    }

    [HttpPatch("comentarios/{id}")]
    [Authorize(Roles = "Jefe,Encargado Departamento")]
    public async Task<ActionResult<ComentarioResponseDto>> EditarComentario(int id, [FromBody] EditarComentarioDto dto)
    {
        try
        {
            var comentario = await _comentarioService.EditarComentarioAsync(
                id,
                dto.Contenido,
                User.ObtenerRol(),
                User.ObtenerDepartamentosIds());

            return Ok(comentario);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("comentarios/{id}")]
    [Authorize(Roles = "Jefe,Encargado Departamento")]
    public async Task<IActionResult> EliminarComentario(int id)
    {
        try
        {
            await _comentarioService.EliminarComentarioAsync(
                id,
                User.ObtenerRol(),
                User.ObtenerDepartamentosIds());

            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

