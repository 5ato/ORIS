using GameAndDot.Shared.Enums;
using GameAndDot.Shared.Interfaces;
using GameAndDot.Shared.Models;

namespace GameAndDot.Shared.Events;

public class DrawStartEvent : IEvent
{
    public EventType Type { get; set; } = EventType.DrawStart;

    public async Task ExecuteAsync(EventMessage message, IServer server, IClientHandler client)
    {
        await server.BroadcastMessageAsync(message, client.Id);
    }
}
