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
        var departamentoId = Context.User!.ObtenerDepartamentoId();

        if (rol == "Jefe")
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "jefes");
        }
        else if (rol == "Encargado Departamento" && departamentoId.HasValue)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"departamento-{departamentoId.Value}");
        }

        await base.OnConnectedAsync();
    }
}