using GestorTareas.API.DTOs.Departamento;
using GestorTareas.API.Services;
using GestorTareas.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace GestorTareas.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class DepartamentoController : ControllerBase
{
    private readonly IDepartamentoService _departamentoService;

    public DepartamentoController(IDepartamentoService departamentoService)
    {
        _departamentoService = departamentoService;
    }

    [HttpPost("crear")]
    [Authorize(Roles = "Jefe")]
    public async Task<ActionResult<DepartamentoResponseDto>> CrearDepartamentoAsync([FromBody] CrearDepartamentoDto request)
    {
        try
        {
            var departamento = await _departamentoService.CrearDepartamentoAsync(request.Nombre!, request.Descripcion!);
            return Ok(departamento);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("obtener")]
    [Authorize(Roles = "Jefe,Encargado Departamento")]
    public async Task<ActionResult<IEnumerable<DepartamentoResponseDto>>> ObtenerDepartamentosAsync()
    {
        var departamentos = await _departamentoService.ObtenerDepartamentosAsync();
        return Ok(departamentos);
    }
}