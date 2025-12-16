using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Signal.Core.Protocol.NMTP
{
    public enum PacketType
    {
        
        NOTIFICATION, // Нужен ли?

        /// <summary>
        /// Тип сообщения пинг, нужен для проверки стабильности соединения
        /// </summary>
        PING,

        /// <summary>
        /// Тип соопщения синхронизация, нужен для синхронизации клиента и сервера при первом подключении
        /// </summary>
        SYNC,

        /// <summary>
        /// Тип сообщения подтверждение, нужен для подтверждения синхронизации между клиентом и сервером
        /// </summary>
        ACK,

        /// <summary>
        /// Тип соопщения подтверждение синхронизации, отправляется сервером клиенту при подтверждении синхронизации
        /// </summary>
        SYNC_ACK,

        /// <summary>
        /// Обычный пакет содержащий какие либо данные
        /// </summary>
        DEFAULT,

        /// <summary>
        /// Нераспознанный пакет
        /// </summary>
        UNNAMED
    }

    public class NMTPPacket
    {
        /// <summary>
        /// Уникальный идентификатор пакета
        /// </summary>
        public Guid PacketId { get; set; }

        /// <summary>
        /// Тип пакета
        /// </summary>
        public PacketType PacketType { get; set; } = PacketType.UNNAMED;

        /// <summary>
        /// Номер пакета в последовательности
        /// </summary>
        public int PacketOrderNumber { get; set; }

        /// <summary>
        /// Общее количество пакетов в запросе
        /// </summary>
        public int TotalPackeges { get; set; }

        /// <summary>
        /// Длина текущего пакета
        /// </summary>
        public long PacketLength { get; set; }

        /// <summary>
        /// Общая длинна запроса
        /// </summary>
        public int TotalLength { get; set; }

        /// <summary>
        /// Определяет защищенность пакета(шифровать или нет)
        /// </summary>
        public bool isProtected { get; set; } = false;

        /// <summary>
        /// Полезная информация пакета(данные)
        /// </summary>
        public List<NMTPField> Fields { get; set; } = new();
    }
}
