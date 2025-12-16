
using GameAndDot.Shared.Implementation;
using GameAndDot.Shared.Models;

namespace GameAndDot.Shared.Interfaces;

public interface IEventProcessor
{
    void RegisterEvent(IEvent myEvent);
    Task ProcessAsync(EventMessage message, IServer server, IClientHandler client);
}
