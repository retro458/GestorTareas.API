using GestorTareas.API.Dtos.Estados;
using GestorTareas.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestorTareas.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EstadosController : ControllerBase
{
    private readonly IEstadoService _estadoService;

    public EstadosController(IEstadoService estadoService)
    {
        _estadoService = estadoService;
    }

    [HttpGet("obtener")]
    public async Task<ActionResult<IEnumerable<EstadoResponseDto>>> ObtenerEstadosAsync()
    {
        var estados = await _estadoService.ObtenerEstadosAsync();
        return Ok(estados);
    }
}
