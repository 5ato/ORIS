using GameAndDot.Shared;
using GameAndDot.Shared.Implementation;

namespace GameAndDot.Server;

internal class Program
{
    static async Task Main(string[] args)
    {
        MyServer server = new MyServer();// создаем сервер
        await server.Start(); // запускаем сервер
    }
}
