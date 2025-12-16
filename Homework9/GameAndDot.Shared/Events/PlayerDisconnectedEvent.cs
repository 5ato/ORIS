using GameAndDot.Shared.Enums;
using GameAndDot.Shared.Implementation;
using GameAndDot.Shared.Interfaces;
using GameAndDot.Shared.Models;
using System.Text.Json;

namespace GameAndDot.Shared.Events;

public class PlayerDisconnectedEvent : IEvent
{
    public EventType Type { get; set; } = EventType.PlayerDisconnected;

    public async Task ExecuteAsync(EventMessage message, IServer server, IClientHandler client)
    {
        Console.WriteLine($"{client.Username} вышёл из чата");

        server.RemoveConnection(client.Id);

        var messageResponse = new EventMessage()
        {
            Type = EventType.PlayerDisconnected,
            Username = client.Username,
            Id = client.Id,
            Players = server.Clients.Values.Select(c => new Player() { Id = c.Id, Username = c.Username, Color = c.Color }).ToArray(),
        };

        await server.BroadcastMessageAsync(messageResponse, client.Id);
    }
}
