using GestorTareas.API.DTOs.Tareas;
using GestorTareas.API.Extensions;
using GestorTareas.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestorTareas.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TareasController : ControllerBase
{
    private readonly ITareaService _tareaService;

    public TareasController(ITareaService tareaService)
    {
        _tareaService = tareaService;
    }

    [HttpPost("crear")]
    [Authorize(Roles = "Jefe,Encargado Departamento")]
    public async Task<ActionResult<TareaResponseDto>> CrearTarea([FromBody] CrearTareaDto dto)
    {
        try
        {
            var tarea = await _tareaService.CrearTareaAsync(
                dto,
                User.ObtenerUsuarioId(),
                User.ObtenerRol(),
                User.ObtenerDepartamentosIds());

            return Ok(tarea);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("obtener")]
    public async Task<ActionResult<IEnumerable<TareaResponseDto>>> ObtenerTareas()
    {
        var tareas = await _tareaService.ObtenerTareasAsync(
            User.ObtenerUsuarioId(),
            User.ObtenerRol(),
            User.ObtenerDepartamentosIds());

        return Ok(tareas);
    }

    [HttpPatch("{id}/estado")]
public async Task<ActionResult<TareaResponseDto>> CambiarEstado(int id, [FromBody] ActualizarEstadoTareaDto dto)
{
    try
    {
        var tarea = await _tareaService.CambiarEstadoAsync(
            id,
            dto.EstadoId,
            User.ObtenerUsuarioId(),
            User.ObtenerRol()); 

        return Ok(tarea);
    }
    catch (UnauthorizedAccessException ex)
    {
        return Forbid(ex.Message);
    }
    catch (Exception ex)
    {
        return BadRequest(new { error = ex.Message });
    }
}

    [HttpPatch("{id}/reasignar")]
    [Authorize(Roles = "Jefe,Encargado Departamento")]
    public async Task<ActionResult<TareaResponseDto>> ReasignarTarea(int id, [FromBody] ReasignarTareaDto dto)
    {
        try
        {
            var tarea = await _tareaService.ReasignarTareaAsync(
                id,
                dto.NuevoAsignadoA,
                User.ObtenerUsuarioId(),
                User.ObtenerRol(),
                User.ObtenerDepartamentosIds());

            return Ok(tarea);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}