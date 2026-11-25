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
    public IActionResult Obtener() => _demo.Activo
        // Las cuentas de demo y sus contrasenas son publicas a proposito: la
        // pantalla de entrada las muestra. Fuera de la demo no se envian.
        ? Ok(new
        {
            activo = true,
            cuentas = DemoSembrador.Cuentas.Select(c => new { usuario = c.Usuario, password = c.Password, rol = c.Rol, nombre = c.Nombre }),
        })
        : Ok(new { activo = false });
}
