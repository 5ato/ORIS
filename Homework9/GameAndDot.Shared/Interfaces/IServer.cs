using GameAndDot.Shared.Implementation;
using GameAndDot.Shared.Models;
using System.Collections.Concurrent;

namespace GameAndDot.Shared.Interfaces;

public interface IServer
{
    bool IsRunning { get; }
    ConcurrentDictionary<string, IClientHandler> Clients { get; set; }
    Task Start(CancellationToken cancellationToken);
    Task Stop();
    Task BroadcastMessageAsync(EventMessage message);
    Task BroadcastMessageAsync(EventMessage message, string id);
    void RemoveConnection(string id);
}
