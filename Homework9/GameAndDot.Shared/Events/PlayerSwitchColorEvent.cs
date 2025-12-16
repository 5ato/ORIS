using GameAndDot.Shared.Enums;
using GameAndDot.Shared.Interfaces;
using GameAndDot.Shared.Models;

namespace GameAndDot.Shared.Events;

public class PlayerSwitchColorEvent : IEvent
{
    public EventType Type { get; set; } = EventType.PlayerSwitchColor;

    public async Task ExecuteAsync(EventMessage message, IServer server, IClientHandler client)
    {
        server.Clients[client.Id].Color = message.Color;
        await server.BroadcastMessageAsync(message, client.Id);
    }
}
