using GestorTareas.API.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace GestorTareas.API.Hubs;

[Authorize]
public class TareasHub : Hub
{
   public override async Task OnConnectedAsync()
{
    var rol = Context.User!.ObtenerRol();
    var departamentosIds = Context.User!.ObtenerDepartamentosIds();

    if (rol == "Jefe")
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "jefes");
    }
    else if (rol == "Encargado Departamento")
    {
        foreach (var deptoId in departamentosIds)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"departamento-{deptoId}");
        }
    }

    await base.OnConnectedAsync();
}
}