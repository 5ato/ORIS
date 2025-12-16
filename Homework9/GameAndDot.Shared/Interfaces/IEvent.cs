using GameAndDot.Shared.Enums;
using GameAndDot.Shared.Implementation;
using GameAndDot.Shared.Models;

namespace GameAndDot.Shared.Interfaces;

public interface IEvent
{
    EventType Type { get; set; }
    Task ExecuteAsync(EventMessage message, IServer server, IClientHandler client);
}
