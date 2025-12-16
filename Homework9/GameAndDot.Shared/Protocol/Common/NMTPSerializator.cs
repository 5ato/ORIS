using GameAndDot.Shared.Models;
using Signal.Core.Protocol.NMTP.Attributes;
using System.Drawing;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace Signal.Core.Protocol.NMTP.Common;

internal static class NMTPSerializator
{
    /// <summary>
    /// Максимальный размер одного пакета который можно отправить за раз
    /// </summary>
    const int MAX_PACKET_SIZE = 100;

    /// <summary>
    /// Байты которые сигнализируют о начале пакета
    /// </summary>
    const byte PACKET_START_BYTE_1 = 0xAD;
    const byte PACKET_START_BYTE_2 = 0xAE;
    const byte PACKET_START_BYTE_3 = 0xAF;

    /// <summary>
    /// Байты которые сигнализируют о конце пакета
    /// </summary>
    const byte PACKET_END_BYTE_1 = 0xEF;
    const byte PACKET_END_BYTE_2 = 0xFF;

    /// <summary>
    /// Количество байт в заголовке пакета NMTP
    /// </summary>
    const int TOTAL_HEADER_SIZE = 50;

    /// <summary>
    /// Сериализует NMTPPacket в байты
    /// </summary>
    /// <param name="packet"></param>
    /// <returns></returns>
    internal static byte[] SerializationPacket(NMTPPacket packet)
    {
        using var packetBuffer = new MemoryStream();
        using var writer = new BinaryWriter(packetBuffer, Encoding.UTF8, true);

        writer.Write([PACKET_START_BYTE_1, PACKET_START_BYTE_2, PACKET_START_BYTE_3]);

        writer.Write(packet.PacketId.ToByteArray());
        writer.Write((int)packet.PacketType);
        writer.Write(packet.PacketOrderNumber);
        writer.Write(packet.TotalPackeges);
        writer.Write(packet.PacketLength);
        writer.Write(packet.TotalLength);
        writer.Write(packet.isProtected);

        writer.Write(packet.Fields.Count);

        foreach (var field in packet.Fields)
        {
            writer.Write(field.FieldId);
            writer.Write(field.FieldSize);
            writer.Write(field.FieldData);
        }

        writer.Write([PACKET_END_BYTE_1, PACKET_END_BYTE_2]);

        return packetBuffer.ToArray();
    }


    /// <summary>
    /// Десериализует байты в NMTPPacket
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    /// <exception cref="InvalidDataException"></exception>
    internal static NMTPPacket DeserializationToPacket(byte[] data)
    {
        using var memoryStream = new MemoryStream(data);
        using var reader = new BinaryReader(memoryStream, Encoding.UTF8, true);

        if (reader.ReadByte() != PACKET_START_BYTE_1 ||
            reader.ReadByte() != PACKET_START_BYTE_2 ||
            reader.ReadByte() != PACKET_START_BYTE_3)
            throw new InvalidDataException("Invalid packet start bytes");

        var packet = new NMTPPacket
        {
            PacketId = new Guid(reader.ReadBytes(16)),
            PacketType = (PacketType)reader.ReadInt32(),
            PacketOrderNumber = reader.ReadInt32(),
            TotalPackeges = reader.ReadInt32(),
            PacketLength = reader.ReadInt64(),
            TotalLength = reader.ReadInt32(),
            isProtected = reader.ReadBoolean(),
        };

        int fieldsCount = reader.ReadInt32();
        var fields = new List<NMTPField>();

        for (int i = 0; i < fieldsCount; i++)
        {
            int fieldId = reader.ReadInt32();
            long fieldSize = reader.ReadInt64();
            byte[] fieldData = reader.ReadBytes((int)fieldSize);
            var field = new NMTPField()
            {
                FieldId = fieldId,
                FieldSize = fieldSize,
                FieldData = fieldData,
            };
            fields.Add(field);
        }

        packet.Fields = fields;

        if (reader.ReadByte() != PACKET_END_BYTE_1 ||
            reader.ReadByte() != PACKET_END_BYTE_2)
            throw new InvalidDataException("Invalid packet end bytes");

        return packet;
    }

    /// <summary>
    /// Сериализует объект(свойства должны быть помечены [FieldAttribute]) в список пакетов NMTP (с учётом фрагментации если нужно)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="data"></param>
    /// <returns></returns>
    internal static List<NMTPPacket> SerializeFromGeneric<T>(T data)
    {
        var fields = new List<NMTPField>();

        var allFields = data?.GetType().GetProperties()
            .Where(t => t.GetCustomAttribute<FieldAttribute>() != null)
            ?? Enumerable.Empty<PropertyInfo>();

        foreach (var property in allFields)
        {
            var fieldAttribute = property.GetCustomAttribute<FieldAttribute>();
            if (fieldAttribute == null) continue;

            var fieldValue = property.GetValue(data);
            byte[] fieldData = Array.Empty<byte>();

            if (fieldValue != null)
            {
                if (property.PropertyType == typeof(Guid) && fieldValue is Guid guid)
                {
                    fieldData = guid.ToByteArray();
                }
                else if (property.PropertyType == typeof(string) && fieldValue is string str)
                {
                    fieldData = Encoding.UTF8.GetBytes(str);
                }
                else if (property.PropertyType == typeof(Dictionary<string, string>) && fieldValue is Dictionary<string, string> dict)
                {
                    var json = System.Text.Json.JsonSerializer.Serialize(dict);
                    fieldData = Encoding.UTF8.GetBytes(json);
                }
                else if (property.PropertyType == typeof(byte[]) && fieldValue is byte[] bytes)
                {
                    fieldData = bytes;
                }
                else if (property.PropertyType == typeof(bool) && fieldValue is bool boolValue)
                {
                    fieldData = BitConverter.GetBytes(boolValue);
                }
                else if (property.PropertyType == typeof(int) && fieldValue is int intValue)
                {
                    fieldData = BitConverter.GetBytes(intValue);
                }
                else if (property.PropertyType == typeof(long) && fieldValue is long longValue)
                {
                    fieldData = BitConverter.GetBytes(longValue);
                }
                else if (property.PropertyType == typeof(short) && fieldValue is short shortValue)
                {
                    fieldData = BitConverter.GetBytes(shortValue);
                }
                else if (property.PropertyType == typeof(float) && fieldValue is float floatValue)
                {
                    fieldData = BitConverter.GetBytes(floatValue);
                }
                else if (property.PropertyType == typeof(double) && fieldValue is double doubleValue)
                {
                    fieldData = BitConverter.GetBytes(doubleValue);
                }
                else if (property.PropertyType.IsEnum)
                {
                    var enumUnderlying = Convert.ToInt32(fieldValue);
                    fieldData = BitConverter.GetBytes(enumUnderlying);
                }
                else if (property.PropertyType.IsValueType && !property.PropertyType.IsPrimitive)
                {
                    // Для структур используем Marshal
                    int size = Marshal.SizeOf(property.PropertyType);
                    IntPtr ptr = Marshal.AllocHGlobal(size);
                    try
                    {
                        Marshal.StructureToPtr(fieldValue, ptr, false);
                        fieldData = new byte[size];
                        Marshal.Copy(ptr, fieldData, 0, size);
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(ptr);
                    }
                }
                else
                {
                    // Для других типов используем JSON сериализацию
                    var json = System.Text.Json.JsonSerializer.Serialize(fieldValue);
                    fieldData = Encoding.UTF8.GetBytes(json);
                }
            }

            var field = new NMTPField
            {
                FieldId = fieldAttribute.FieldId,
                FieldSize = fieldData.Length,
                FieldData = fieldData
            };

            fields.Add(field);
        }

        return FragmentPacket(fields, PacketType.DEFAULT);
    }

    /// <summary>
    /// Фрагментирует пакет засчёт филдов на несколько пакетов если он превышает максимальный размер
    /// </summary>
    /// <param name="allFields"></param>
    /// <param name="packetType"></param>
    /// <returns></returns>
    private static List<NMTPPacket> FragmentPacket(List<NMTPField> allFields, PacketType packetType)
    {
        var packets = new List<NMTPPacket>();
        var sessionId = Guid.NewGuid();

        var maxFieldSize = MAX_PACKET_SIZE - TOTAL_HEADER_SIZE;

        var currentFields = new List<NMTPField>();
        int currentSize = 0;

        foreach (var field in allFields)
        {
            int fieldSize = 4 + 8 + (int)field.FieldSize;

            if (fieldSize > maxFieldSize)
            {
                if (currentFields.Count > 0)
                {
                    packets.Add(CreatePacket(sessionId, currentFields, packetType, packets.Count + 1, 0));
                    currentFields = new List<NMTPField>();
                    currentSize = 0;
                }

                var fragmentedPackets = FragmentLargeField(sessionId, field, packetType, packets.Count + 1);
                packets.AddRange(fragmentedPackets);
            }
            else if (currentSize + fieldSize > maxFieldSize)
            {
                packets.Add(CreatePacket(sessionId, currentFields, packetType, packets.Count + 1, 0));
                currentFields = new List<NMTPField>() { field };
                currentSize = fieldSize;
            }
            else
            {
                currentFields.Add(field);
                currentSize += fieldSize;
            }
        }

        if (currentFields.Count > 0)
            packets.Add(CreatePacket(sessionId, currentFields, packetType, packets.Count + 1, 0));

        // Обновляем TotalPackages и TotalLength для всех пакетов
        int totalLength = 0;
        foreach (var packet in packets)
        {
            totalLength += (int)packet.PacketLength;
        }

        foreach (var packet in packets)
        {
            packet.TotalPackeges = packets.Count;
            packet.TotalLength = totalLength;
        }

        return packets;
    }

    /// <summary>
    /// Фрагментирует большое поле на несколько пакетов
    /// </summary>
    /// <param name="sessionId">Id одного пакета</param>
    /// <param name="largeField"></param>
    /// <param name="type"></param>
    /// <param name="startOrderNumber"></param>
    /// <returns></returns>
    private static List<NMTPPacket> FragmentLargeField(Guid sessionId, NMTPField largeField, PacketType type, int startOrderNumber)
    {
        var packets = new List<NMTPPacket>();

        int maxDataSize = MAX_PACKET_SIZE - TOTAL_HEADER_SIZE - 4 - 8;

        int offset = 0;
        int remainingData = (int)largeField.FieldSize;

        while (remainingData > 0)
        {
            int chunkSize = Math.Min(remainingData, maxDataSize);
            var chunkData = new byte[chunkSize];
            Array.Copy(largeField.FieldData, offset, chunkData, 0, chunkSize);

            var field = new NMTPField
            {
                FieldId = largeField.FieldId,
                FieldSize = chunkSize,
                FieldData = chunkData
            };

            packets.Add(CreatePacket(sessionId, new List<NMTPField> { field }, type, startOrderNumber + packets.Count, 0));

            offset += chunkSize;
            remainingData -= chunkSize;
        }

        return packets;
    }

    private static NMTPPacket CreatePacket(Guid sessionId, List<NMTPField> fields, PacketType type, int orderNumber, int totalPackages)
    {
        var packet = new NMTPPacket()
        {
            PacketId = sessionId,
            PacketType = type,
            PacketOrderNumber = orderNumber,
            TotalPackeges = totalPackages,
            Fields = fields,
            isProtected = false,
        };

        packet.PacketLength = CalculatePacketLength(packet);

        return packet;
    }

    /// <summary>
    /// Высчитывает длину пакета. Длина заголовка + сумма всех полей (Id филда - 4 байта, FieldSize длина филда - 8 байт, Сам FieldSize - n байта)
    /// </summary>
    /// <param name="packet"></param>
    /// <returns></returns>
    private static long CalculatePacketLength(NMTPPacket packet)
    {
        long total = TOTAL_HEADER_SIZE;
        foreach (var field in packet.Fields)
        {
            total += 4 + 8 + field.FieldSize;
        }
        return total;
    }

    internal static T DeserializationFromGeneric<T>(List<NMTPPacket> packeges) where T : new()
    {
        var instance = new T();

        // Собираем свойства, помеченные атрибутом с FieldId -> Dictionary<FieldId, PropertyInfo>
        var properties = typeof(T)
            .GetProperties()
            .Select(p => new { Prop = p, Attr = p.GetCustomAttribute<FieldAttribute>() })
            .Where(x => x.Attr != null)
            .ToDictionary(x => x.Attr.FieldId, x => x.Prop);

        // Собираем все поля из пакетов в порядке номера пакета и порядка в пакете,
        // конкатенируя данные по FieldId (вместе собираем фрагменты больших полей).
        var fieldBuffers = new Dictionary<int, List<byte>>();

        foreach (var packet in packeges.OrderBy(p => p.PacketOrderNumber))
        {
            foreach (var field in packet.Fields)
            {
                if (!fieldBuffers.TryGetValue(field.FieldId, out var list))
                {
                    list = new List<byte>();
                    fieldBuffers[field.FieldId] = list;
                }
                list.AddRange(field.FieldData);
            }
        }

        // Теперь для каждого собранного FieldId пытаемся найти соответствующее свойство и десериализовать
        foreach (var kv in fieldBuffers)
        {
            int fieldId = kv.Key;
            byte[] data = kv.Value.ToArray();

            if (!properties.TryGetValue(fieldId, out var property))
            {
                // Нет свойства с таким FieldId — игнорируем
                continue;
            }

            var targetType = property.PropertyType;
            object? value = null;

            try
            {
                // Обработка известных типов (сопоставлена с SerializeFromGeneric).
                if (targetType == typeof(Guid))
                {
                    value = new Guid(data);
                }
                else if (targetType == typeof(string))
                {
                    value = Encoding.UTF8.GetString(data);
                }
                else if (targetType == typeof(Player[]))
                {
                    var json = Encoding.UTF8.GetString(data);
                    value = JsonSerializer.Deserialize<Player[]>(json);
                }
                else if (targetType == typeof(Dictionary<string, string>))
                {
                    var json = Encoding.UTF8.GetString(data);
                    value = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                }
                else if (targetType == typeof(byte[]))
                {
                    value = data;
                }
                else if (targetType == typeof(bool))
                {
                    value = BitConverter.ToBoolean(data, 0);
                }
                else if (targetType == typeof(int))
                {
                    value = BitConverter.ToInt32(data, 0);
                }
                else if (targetType == typeof(long))
                {
                    value = BitConverter.ToInt64(data, 0);
                }
                else if (targetType == typeof(short))
                {
                    value = BitConverter.ToInt16(data, 0);
                }
                else if (targetType == typeof(float))
                {
                    value = BitConverter.ToSingle(data, 0);
                }
                else if (targetType == typeof(double))
                {
                    value = BitConverter.ToDouble(data, 0);
                }
                else if (targetType.IsEnum)
                {
                    // попытка распарсить как int и преобразовать в enum
                    var enumUnderlying = BitConverter.ToInt32(data, 0);
                    value = Enum.ToObject(targetType, enumUnderlying);
                }
                else if (targetType.IsValueType && !targetType.IsPrimitive)
                {
                    // Структура: используем Marshal (как при сериализации вы применяли Marshal)
                    int size = Marshal.SizeOf(targetType);
                    IntPtr ptr = Marshal.AllocHGlobal(size);
                    try
                    {
                        // Если пришло меньше байт, чем размер структуры — обнулим остальное
                        var src = new byte[size];
                        Array.Copy(data, 0, src, 0, Math.Min(data.Length, size));
                        Marshal.Copy(src, 0, ptr, size);
                        value = Marshal.PtrToStructure(ptr, targetType);
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(ptr);
                    }
                }
                else
                {
                    // По умолчанию — десериализация из JSON (сериалайзер использовал JSON для "прочих" типов).
                    var json = Encoding.UTF8.GetString(data);
                    value = JsonSerializer.Deserialize(json, targetType);
                }

                // Устанавливаем значение
                property.SetValue(instance, value);
            }
            catch (Exception ex)
            {
                // Можно логировать ошибку или пробросить с более информативным сообщением.
                throw new InvalidOperationException($"Не удалось десериализовать поле #{fieldId} в свойство '{property.Name}' типа {targetType.FullName}", ex);
            }
        }

        return instance;
    }
}
