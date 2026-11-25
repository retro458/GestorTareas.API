using GestorTareas.API.Demo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestorTareas.API.Controllers;

// Le dice al front si debe mostrarse en modo demo (franja, cuentas de prueba...).
[ApiController]
[Route("api/demo")]
[AllowAnonymous]
public class DemoController : ControllerBase
{
    private readonly DemoModo _demo;

    public DemoController(DemoModo demo) => _demo = demo;

    [HttpGet]
    public IActionResult Obtener() => Ok(new { activo = _demo.Activo });
}
