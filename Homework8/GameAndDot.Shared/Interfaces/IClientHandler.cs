
using System.Net.Sockets;

namespace GameAndDot.Shared.Interfaces;

public interface IClientHandler
{
    int Color { get; set; }
    string Username { get; set; }
    IServer ServerSocket { get; set; }
    Socket ClientSocket { get; set; }
    string Id { get; set; }
    Task HandleClientAsync(CancellationToken cancellationToken);
    void Dispose();
}
