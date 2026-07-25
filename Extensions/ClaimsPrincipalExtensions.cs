using System.Security.Claims;

namespace GestorTareas.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static int ObtenerUsuarioId(this ClaimsPrincipal user)
    {
        var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (idClaim is null || !int.TryParse(idClaim, out var id))
            throw new UnauthorizedAccessException("No se pudo determinar el usuario actual desde el token.");
        return id;
    }

    public static string ObtenerRol(this ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
    }

    public static int? ObtenerDepartamentoId(this ClaimsPrincipal user)
    {
        var deptoClaim = user.FindFirst("DepartamentoId")?.Value;
        return deptoClaim != null && int.TryParse(deptoClaim, out var id) ? id : null;
    }
}