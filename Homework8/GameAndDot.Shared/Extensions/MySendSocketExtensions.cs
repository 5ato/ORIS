
using GameAndDot.Shared.Models;
using System.Net.Sockets;
using System.Text.Json;

namespace GameAndDot.Shared.Extensions;

public static class MySendSocketExtensions
{
    public async static Task<EventMessage> ReadEventMessage(this Socket socket)
    {
        byte[] lengthBuffer = new byte[4];
        await ReceiveAllAsync(socket, lengthBuffer, 4);

        int bodyLength = BitConverter.ToInt32(lengthBuffer);

        if (bodyLength <= 0)
        {
            throw new InvalidDataException("Invalid message length");
        }

        byte[] bodyBuffer = new byte[bodyLength];
        await ReceiveAllAsync(socket, bodyBuffer, bodyLength);

        return JsonSerializer.Deserialize<EventMessage>(bodyBuffer)!;
    }

    public async static Task SendEventMessage(this Socket socket, EventMessage message)
    {
        byte[] body = JsonSerializer.SerializeToUtf8Bytes(message);

        byte[] fullMessage = new byte[4 + body.Length];

        BitConverter.GetBytes(body.Length).CopyTo(fullMessage, 0);
        Buffer.BlockCopy(body, 0, fullMessage, 4, body.Length);

        await socket.SendAsync(fullMessage, SocketFlags.None);
    }

    private static async Task ReceiveAllAsync(Socket socket, byte[] buffer, int size)
    {
        int totalReceived = 0;

        while (totalReceived < size)
        {
            int received = await socket.ReceiveAsync(
                new ArraySegment<byte>(buffer, totalReceived, size - totalReceived),
                SocketFlags.None
            );

            if (received == 0)
            {
                throw new SocketException((int)SocketError.ConnectionReset);
            }

            totalReceived += received;
        }
    }
}
