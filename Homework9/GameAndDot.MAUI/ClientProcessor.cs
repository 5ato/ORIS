using GameAndDot.Shared.Enums;
using GameAndDot.Shared.Extensions;
using GameAndDot.Shared.Models;
using System.Net.Sockets;

namespace GameAndDot.MAUI;

public class ClientProcessor
{
    private readonly Socket _client;
    private string host = "127.0.0.1";
    private int port = 8888;

    private readonly string Id;

    public ClientProcessor(string id)
    {
        Id = id;
        _client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        try
        {
            _client.Connect(host, port); //подключение клиента
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    public async Task ConnectClient(UserInfo user, Action<EventMessage> action)
    {
        await _client.ConnectAsync();

        _ = Task.Run(async () => await ReceiveMessageAsync(action));

        var message = new EventMessage()
        {
            Type = EventType.PlayerConnected,
            Username = user.Username,
            Color = user.DotColor.ToInt(),
            Id = Id
        };

        await SendMessageAsync(message);
    }

    public async Task SendMessageAsync(EventMessage message)
    {
        await _client.SendPacket<EventMessage>(message);
    }

    private async Task ReceiveMessageAsync(Action<EventMessage> action)
    {
        while (true)
        {
            try
            {
                var messageRequest = await _client.RecivePacket<EventMessage>();

                if (messageRequest == null) break;

                action(messageRequest);
            }
            catch
            {
                break;
            }
        }
    }

    public async Task DisconnectAsync()
    {
        var message = new EventMessage()
        {
            Type = EventType.PlayerDisconnected,
            Id = Id
        };
        await SendMessageAsync(message);

        Dispose();
    }

    public void Dispose()
    {
        _client?.Dispose();
    }
}
