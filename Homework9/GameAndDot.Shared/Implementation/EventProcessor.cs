using GameAndDot.Shared.Enums;
using GameAndDot.Shared.Interfaces;
using GameAndDot.Shared.Models;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;

namespace GameAndDot.Shared.Implementation;

public class EventProcessor : IEventProcessor
{
    private static IEventProcessor? _instance = null;
    private static readonly Lock _lock = new();

    public static IEventProcessor Instance
    {
        get
        {
            lock (_lock)
            {
                _instance ??= new EventProcessor();
            }
            return _instance;
        }
    }


    private EventProcessor()
    {
        var currentAssembly = Assembly.GetExecutingAssembly();
        _events = currentAssembly.GetTypes()
                .Where(t => typeof(IEvent).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                .Select(t => (IEvent)Activator.CreateInstance(t)!)
                .ToDictionary(e => e.Type, e => e);
    }

    private readonly Dictionary<EventType, IEvent> _events = [];
    private readonly Lock _eventlock = new();

    public async Task ProcessAsync(EventMessage message, IServer server, IClientHandler client)
    {

        if (!_events.TryGetValue(message.Type, out var myEvent))
        {
            throw new KeyNotFoundException($"Event of type {message.Type} is not registered.");
        }

        try
        {
            await myEvent.ExecuteAsync(message, server, client);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error executing event of type {message.Type}: {ex.Message}", ex);

        }
    }

    public void RegisterEvent(IEvent myEvent)
    {
        lock (_eventlock)
        {
            _events[myEvent.Type] = myEvent;
        }
    }
}
