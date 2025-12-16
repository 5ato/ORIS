using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Signal.Core.Protocol.NMTP
{
    public class NMTPField
    {
        public int FieldId { get; set; }

        public long FieldSize { get; set; }

        public byte[] FieldData { get; set; } = Array.Empty<byte>();
    }
}
