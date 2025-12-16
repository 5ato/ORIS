
using GameAndDot.Shared.Models;
using Signal.Core.Protocol.NMTP;
using Signal.Core.Protocol.NMTP.Common;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace GameAndDot.Shared.Extensions;

public static class MySendSocketExtensions
{
    /// <summary>
    /// Количество байт в заголовке пакета NMTP
    /// </summary>
    const int TOTAL_HEADER_SIZE = 50;

    /// <summary>
    /// Метод для установки соединения через SYNC, SYNC_ACK и ACK пакеты на клиенте
    /// </summary>
    public static async Task ConnectAsync(this Socket socket)
    {
        // Формируем SYNC пакет и отправляем его
        var syncPacket = new NMTPPacket
        {
            PacketId = Guid.NewGuid(),
            PacketType = PacketType.SYNC,
            PacketOrderNumber = 1,
            TotalPackeges = 1,
            PacketLength = TOTAL_HEADER_SIZE,
            TotalLength = 0,
            isProtected = false,

            Fields = []
        };

        byte[] syncPacketBuffer = NMTPSerializator.SerializationPacket(syncPacket);

        await SendAllAsync(socket, syncPacketBuffer);
        Console.WriteLine("SYNC sent");

        // Ждём SYNC_ACK пакет от сервера
        var responsePacket = await ReceiveSinglePacket(socket);

        if (responsePacket.PacketType != PacketType.SYNC_ACK)
            throw new InvalidOperationException("Invalid packet type received during connection establishment.");
        Console.WriteLine("SYNC_ACK received");

        // Формируем ACK пакет и отправляем его
        var ackPacket = new NMTPPacket()
        {
            PacketId = Guid.NewGuid(),
            PacketType = PacketType.ACK,
            PacketOrderNumber = 1,
            TotalPackeges = 1,
            PacketLength = TOTAL_HEADER_SIZE,
            TotalLength = 0,
            isProtected = false,

            Fields = []
        };

        byte[] ackBuffer = NMTPSerializator.SerializationPacket(ackPacket);

        await SendAllAsync(socket, ackBuffer);
        Console.WriteLine("ACK sent");
    }

    /// <summary>
    /// Ожидает входящее соединение и устанавливает его через SYNC, SYNC_ACK и ACK пакеты на сервере
    /// </summary>
    /// <param name="socket"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static async Task NMTPAcceptAsync(this Socket socket)
    {
        // Ждём SYNC пакет от клиента
        var syncPacket = await ReceiveSinglePacket(socket);
        if (syncPacket.PacketType != PacketType.SYNC)
            throw new InvalidOperationException("Invalid packet type received during connection establishment.");

        Console.WriteLine("SYNC received");

        // Формируем SYNC_ACK пакет и отправляем его
        var syncAckPacket = new NMTPPacket()
        {
            PacketId = Guid.NewGuid(),
            PacketType = PacketType.SYNC_ACK,
            PacketOrderNumber = 1,
            TotalPackeges = 1,
            PacketLength = TOTAL_HEADER_SIZE,
            TotalLength = 0,
            isProtected = false,
            Fields = []
        };

        var syncAckBytes = NMTPSerializator.SerializationPacket(syncAckPacket);
        await SendAllAsync(socket, syncAckBytes);
        Console.WriteLine("SYNC_ACK sent");

        // Ждём ACK пакет от клиента
        var ackPacket = await ReceiveSinglePacket(socket);

        if (ackPacket.PacketType != PacketType.ACK)
            throw new InvalidOperationException("Expected ACK packet");

        Console.WriteLine("ACK received");
    }

    /// <summary>
    /// Отправляет пакет NMTP с данными типа T (чтобы свойства типа T были помечены атрибутом [FieldAttribute])
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="socket"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    public static async Task SendPacket<T>(this Socket socket, T data)
    {
        // Получаем множество пакетов с одного объекта (если объект тяжёлый)
        var packets = NMTPSerializator.SerializeFromGeneric<T>(data);

        foreach (var packet in packets)
        {
            var packetByte = NMTPSerializator.SerializationPacket(packet);
            await SendAllAsync(socket, packetByte);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="socket"></param>
    /// <returns></returns>
    public static async Task<T> RecivePacket<T>(this Socket socket) where T : new()
    {
        var packets = await ReceiveMultiplePacketsAsync(socket);
        return NMTPSerializator.DeserializationFromGeneric<T>(packets);
    }

    /// <summary>
    /// Отправляет все пакеты в формате байтов
    /// </summary>
    /// <param name="socket"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    /// <exception cref="SocketException"></exception>
    private static async Task SendAllAsync(Socket socket, byte[] data)
    {
        int totalSend = 0;
        int remaining = data.Length;

        while (remaining > 0)
        {
            int sent = await socket.SendAsync(new ArraySegment<byte>(data, totalSend, remaining), SocketFlags.None);

            if (sent == 0)
                throw new SocketException((int)SocketError.ConnectionReset);

            totalSend += sent;
            remaining -= sent;
        }
    }

    /// <summary>
    /// Принимает байты и сериализует их в пакет
    /// </summary>
    /// <param name="socket"></param>
    /// <returns></returns>
    private static async Task<NMTPPacket> ReceiveSinglePacket(Socket socket)
    {
        var headerBuffer = new byte[TOTAL_HEADER_SIZE];
        await ReceiveExactAsync(socket, headerBuffer, headerBuffer.Length);

        // Парсим PacketLength из заголовка
        using var ms = new MemoryStream(headerBuffer);
        using var reader = new BinaryReader(ms, Encoding.UTF8, true);

        // Пропускаем начальные байты и GUID
        reader.ReadBytes(3 + 16 + 4 + 4 + 4); // Первые три байта + PacketId + PacketType + OrderNumber + TotalPackages
        long packetLength = reader.ReadInt64(); // PacketLength

        // Теперь читаем весь пакет целиком
        var fullPacketBuffer = new byte[packetLength];
        Array.Copy(headerBuffer, fullPacketBuffer, headerBuffer.Length);

        int remaining = (int)packetLength - headerBuffer.Length;
        if (remaining > 0)
        {
            await ReceiveExactAsync(socket, fullPacketBuffer, remaining, headerBuffer.Length);
        }

        return NMTPSerializator.DeserializationToPacket(fullPacketBuffer);
    }

    /// <summary>
    /// Получаем байты и сериализуем их в пакеты
    /// </summary>
    /// <param name="socket"></param>
    /// <returns></returns>
    /// <exception cref="InvalidDataException"></exception>
    private static async Task<List<NMTPPacket>> ReceiveMultiplePacketsAsync(Socket socket)
    {
        var packets = new List<NMTPPacket>();

        // Получаем первый пакет для анализа сколько нам ещё нужно принять
        var firstPacket = await ReceiveSinglePacket(socket);
        packets.Add(firstPacket);

        if (firstPacket.TotalPackeges == 1)
            return packets;

        // Мы всегда отправляем только один объект, а он может преобразоваться в множество пакетов, и у всех этих пакетов есть только один id
        var sessionId = firstPacket.PacketId;
        var expectedPackeges = firstPacket.TotalPackeges;

        // Просто перебиаем пакеты
        while (packets.Count < expectedPackeges)
        {
            var packet = await ReceiveSinglePacket(socket);

            if (packet.PacketId != sessionId)
                throw new InvalidDataException($"Received packet from different session. Expected {sessionId}, got {packet.PacketId}");

            packets.Add(packet);
        }

        // Проверяем, что получили все пакеты
        var orderNumbers = packets.Select(p => p.PacketOrderNumber).OrderBy(n => n).ToList();
        for (int i = 1; i <= expectedPackeges; i++)
        {
            if (!orderNumbers.Contains(i))
            {
                throw new InvalidDataException($"Missing packet {i} of {expectedPackeges}");
            }
        }

        return packets;
    }

    /// <summary>
    /// Простое чтение байтов по заданному количеству
    /// </summary>
    /// <param name="socket"></param>
    /// <param name="buffer"></param>
    /// <param name="count"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    /// <exception cref="SocketException"></exception>
    private static async Task ReceiveExactAsync(Socket socket, byte[] buffer, int count, int offset = 0)
    {
        int totalRead = 0;

        while (totalRead < count)
        {
            int read = await socket.ReceiveAsync(new ArraySegment<byte>(buffer, offset + totalRead, count - totalRead), SocketFlags.None);
            if (read == 0)
                throw new SocketException((int)SocketError.ConnectionReset);
            totalRead += read;
        }
    }

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
