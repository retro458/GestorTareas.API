using GestorTareas.API.Data;
using GestorTareas.API.DTOs.ComentariosDto;
using GestorTareas.API.DTOs.Departamento;
using GestorTareas.API.DTOs.Tareas;
using GestorTareas.API.DTOs.Usuarios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace GestorTareas.API.Demo;

// ============================================================
// Reglas extra del modo demo publico.
// ============================================================
// Este filtro solo se registra con Demo__Activo=true (ver Program.cs), asi que
// los servicios y controladores de produccion no cambian. Hace tres cosas:
//   1. Protege las cuentas de demo: no se editan, desactivan ni se les cambia
//      la contrasena (403 "No disponible en la demo.").
//   2. Topes para que nadie llene la base: tareas, comentarios y usuarios.
//   3. Largo maximo de los textos que llegan a la API.
public sealed class DemoFiltro : IAsyncActionFilter
{
    public const int MaxTareas = 200;
    public const int MaxComentarios = 300;
    public const int MaxUsuariosCreados = 20;

    public const string MensajeNoDisponible = "No disponible en la demo.";
    public const string MensajeLimite = "La demo llegó a su límite; se reinicia esta noche.";

    // (DTO, propiedad) -> largo maximo. Los que tienen columna con largo en la
    // base usan ese mismo largo; los de texto libre (nvarchar(max)) uno razonable.
    private static readonly Dictionary<(Type, string), int> LargosMaximos = new()
    {
        [(typeof(CrearTareaDto), nameof(CrearTareaDto.Titulo))] = 200,
        [(typeof(CrearTareaDto), nameof(CrearTareaDto.Descripcion))] = 2000,
        [(typeof(AutoAsignarTareaDto), nameof(AutoAsignarTareaDto.Titulo))] = 200,
        [(typeof(AutoAsignarTareaDto), nameof(AutoAsignarTareaDto.Descripcion))] = 2000,
        [(typeof(EditarTareaDto), nameof(EditarTareaDto.Titulo))] = 200,
        [(typeof(EditarTareaDto), nameof(EditarTareaDto.Descripcion))] = 2000,
        [(typeof(CrearComentarioDto), nameof(CrearComentarioDto.Contenido))] = 1000,
        [(typeof(EditarComentarioDto), nameof(EditarComentarioDto.Contenido))] = 1000,
        [(typeof(CrearUsuariosDto), nameof(CrearUsuariosDto.Nombre))] = 150,
        [(typeof(CrearUsuariosDto), nameof(CrearUsuariosDto.NombreUsuario))] = 50,
        [(typeof(CrearUsuariosDto), nameof(CrearUsuariosDto.Password))] = 100,
        [(typeof(EditarUsuarioDto), nameof(EditarUsuarioDto.NombreUsuario))] = 50,
        [(typeof(RestablecerPasswordDto), nameof(RestablecerPasswordDto.NuevaPassword))] = 100,
        [(typeof(CrearDepartamentoDto), nameof(CrearDepartamentoDto.Nombre))] = 100,
        [(typeof(CrearDepartamentoDto), nameof(CrearDepartamentoDto.Descripcion))] = 255,
        [(typeof(EditarDepartamentoDto), nameof(EditarDepartamentoDto.Nombre))] = 100,
        [(typeof(EditarDepartamentoDto), nameof(EditarDepartamentoDto.Descripcion))] = 255,
    };

    // Acciones de UsuarioController que modifican la cuenta del {id} de la ruta.
    private static readonly HashSet<string> AccionesSobreUsuario = new()
    {
        "EditarUsuario", "DesactivarUsuario", "RestablecerPassword",
    };

    private readonly AppDbContext _db;

    public DemoFiltro(AppDbContext db) => _db = db;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.ActionDescriptor is not ControllerActionDescriptor accion)
        {
            await next();
            return;
        }

        var rechazo = VerificarLargos(context)
            ?? await VerificarCuentaProtegidaAsync(context, accion)
            ?? await VerificarTopesAsync(accion);

        if (rechazo is not null)
        {
            context.Result = rechazo;
            return;
        }

        await next();
    }

    private static IActionResult? VerificarLargos(ActionExecutingContext context)
    {
        foreach (var argumento in context.ActionArguments.Values)
        {
            if (argumento is null) continue;
            var tipo = argumento.GetType();
            foreach (var propiedad in tipo.GetProperties())
            {
                if (propiedad.PropertyType != typeof(string)) continue;
                if (!LargosMaximos.TryGetValue((tipo, propiedad.Name), out var maximo)) continue;
                if (propiedad.GetValue(argumento) is string valor && valor.Length > maximo)
                    return new BadRequestObjectResult(new { error = $"El campo {propiedad.Name} admite como máximo {maximo} caracteres." });
            }
        }
        return null;
    }

    private async Task<IActionResult?> VerificarCuentaProtegidaAsync(ActionExecutingContext context, ControllerActionDescriptor accion)
    {
        if (accion.ControllerName != "Usuario" || !AccionesSobreUsuario.Contains(accion.MethodInfo.Name))
            return null;
        if (!context.ActionArguments.TryGetValue("id", out var valorId) || valorId is not int id)
            return null;

        var nombreUsuario = await _db.Usuarios.Where(u => u.Id == id).Select(u => u.NombreUsuario).FirstOrDefaultAsync();
        return DemoSembrador.EsCuentaProtegida(nombreUsuario)
            ? new ObjectResult(new { error = MensajeNoDisponible }) { StatusCode = StatusCodes.Status403Forbidden }
            : null;
    }

    private async Task<IActionResult?> VerificarTopesAsync(ControllerActionDescriptor accion)
    {
        var llenoDe = (accion.ControllerName, accion.MethodInfo.Name) switch
        {
            ("Tareas", "CrearTarea") or ("Tareas", "AutoAsignarTarea") =>
                await _db.Tareas.CountAsync() >= MaxTareas,
            ("Comentarios", "CrearComentario") =>
                await _db.ComentariosTarea.CountAsync() >= MaxComentarios,
            ("Usuario", "RegisterAsync") =>
                await _db.Usuarios.CountAsync() >= DemoSembrador.UsuariosSembrados + MaxUsuariosCreados,
            _ => false,
        };
        return llenoDe ? new ConflictObjectResult(new { error = MensajeLimite }) : null;
    }
}
