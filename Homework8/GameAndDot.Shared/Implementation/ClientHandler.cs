
using GameAndDot.Shared.Enums;
using GameAndDot.Shared.Extensions;
using GameAndDot.Shared.Interfaces;
using GameAndDot.Shared.Models;
using System.Net.Sockets;
using System.Text.Json;

namespace GameAndDot.Shared.Implementation;

public class ClientHandler : IClientHandler
{
    private readonly IEventProcessor _eventProcessor;

    public Socket ClientSocket { get; set; }
    public IServer ServerSocket { get; set; }

    public string Username { get; set; }
    public string Id { get; set; } = string.Empty;
    public int Color { get; set; }

    public ClientHandler(IEventProcessor eventProcessor, Socket clientSocket, IServer serverSocket)
    {
        _eventProcessor = eventProcessor;

        ClientSocket = clientSocket;
        ServerSocket = serverSocket;

    }

    public void Dispose()
    {
        ClientSocket.Dispose();
    }

    public async Task HandleClientAsync(CancellationToken cancellationToken)
    {
        
        try
        {
            while (!cancellationToken.IsCancellationRequested && ClientSocket.Connected)
            {
                if (!ClientSocket.Connected)
                    break;

                var data = await ClientSocket.ReadEventMessage();

                if (data == null) break;

                await _eventProcessor.ProcessAsync(data, ServerSocket, this);
            }
        }
        catch (OperationCanceledException)
        {

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling client {Id}: {ex.Message}");
            ClientSocket.Dispose();
            ServerSocket.RemoveConnection(Id);
            await ServerSocket.BroadcastMessageAsync(new EventMessage()
            {
                Type = EventType.PlayerDisconnected,
                Username = Username,
                Id = Id,
                Players = ServerSocket.Clients.Values.Select(c => new Player() { Username = c.Username, Color = c.Color }).ToArray(),
            });
        }
    }
}
