
using GameAndDot.Shared.Enums;
using GameAndDot.Shared.Implementation;
using GameAndDot.Shared.Interfaces;
using GameAndDot.Shared.Models;
using System.Text.Json;

namespace GameAndDot.Shared.Events;

public class PlayerConnectedEvent : IEvent
{
    public EventType Type { get ; set ; } = EventType.PlayerConnected;

    public async Task ExecuteAsync(EventMessage message, IServer server, IClientHandler client)
    {
        client.Color = message.Color;
        client.Username = message.Username;
        client.Id = message.Id;
        server.Clients[client.Id] = client;

        Console.WriteLine($"{message.Username} вошёл в чат");

        var messageResponse = new EventMessage()
        {
            Type = EventType.PlayerConnected,
            Username = message.Username,
            Id = message.Id,
            Color = message.Color,
            Players = server.Clients.Values.Select(c => new Player() { Id = c.Id, Username = c.Username, Color = c.Color }).ToArray(),
        };

        await server.BroadcastMessageAsync(messageResponse);
    }
}
