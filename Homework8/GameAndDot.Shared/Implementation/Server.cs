
using GameAndDot.Shared.Enums;
using GameAndDot.Shared.Extensions;
using GameAndDot.Shared.Interfaces;
using GameAndDot.Shared.Models;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;

namespace GameAndDot.Shared.Implementation;

public class MyServer : IServer
{
    private Socket? _listener;
    private CancellationTokenSource? _cancellationTokenSource;
    private readonly IEventProcessor _eventProcessor = EventProcessor.Instance;
    public ConcurrentDictionary<string, IClientHandler> Clients { get; set; } = [];

    public bool IsRunning { get; set; }

    public async Task Start(CancellationToken cancellationToken = default)
    {
        if (IsRunning) return;

        _listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        _listener.Bind(new IPEndPoint(IPAddress.Any, 8888));

        _listener.Listen(100);

        IsRunning = true;

        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        await AcceptClientsAsync(_cancellationTokenSource.Token);
    }

    private async Task AcceptClientsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var clientSocket = await _listener!.AcceptAsync(cancellationToken);

                var clientHandler = new ClientHandler(_eventProcessor, clientSocket, this);

                _ = Task.Run(async () => await clientHandler.HandleClientAsync(cancellationToken), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error accepting client: {ex.Message}");
            }
        }
    }

    public async Task BroadcastMessageAsync(EventMessage message, string id)
    {
        foreach (var client in Clients.Values)
        {
            if (client.Id != id) // если id клиента не равно id отправителя
            {
                await client.ClientSocket.SendEventMessage(message); //передача данных
            }
        }
    }

    public async Task BroadcastMessageAsync(EventMessage message)
    {
        foreach (var client in Clients.Values)
        {
            await client.ClientSocket.SendEventMessage(message); //передача данных
        }
    }

    public void RemoveConnection(string id)
    {
        IClientHandler? client = Clients.FirstOrDefault(c => c.Key == id).Value;

        if (client != null)
        {
            Clients.Remove(client.Id, out client);
        }

        client?.Dispose();
    }

    public async Task Stop()
    {
        if (!IsRunning) return;

        _cancellationTokenSource?.Cancel();

        if (_listener != null)
        {
            _listener.Close();
            _listener = null;
        }

        try
        {
            foreach (var client in Clients.Values)
            {
                client.Dispose(); //отключение клиента
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error disconnecting clients: {ex.Message}");
        }

        IsRunning = false;
    }
}
