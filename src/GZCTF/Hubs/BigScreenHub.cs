using System.Diagnostics.CodeAnalysis;
using GZCTF.Hubs.Clients;
using Microsoft.AspNetCore.SignalR;

namespace GZCTF.Hubs;

[ExcludeFromCodeCoverage]
public class BigScreenHub : Hub<IBigScreenClient>
{
    public override async Task OnConnectedAsync()
    {
        var context = Context.GetHttpContext();
        if (context is null)
        {
            Context.Abort();
            return;
        }

        await base.OnConnectedAsync();
        await Groups.AddToGroupAsync(Context.ConnectionId, "BigScreen");
    }
}
