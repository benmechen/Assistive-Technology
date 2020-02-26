using System;
using System.IO;

namespace Zeroconf
{
    public class BigEndianReader : BinaryReader
    {
        public BigEndianReader(System.IO.Stream stream) : base(stream)
        {
        }

        public BigEndianReader(byte[] data) : base(new MemoryStream(data))
        {
        }

        public override Int16 ReadInt16()
        {
            var data = base.ReadBytes(2);
            Array.Reverse(data);
            return BitConverter.ToInt16(data, 0);
        }

        public override int ReadInt32()
        {
            var data = base.ReadBytes(4);
            Array.Reverse(data);
            return BitConverter.ToInt32(data, 0);
        }

        public override Int64 ReadInt64()
        {
            var data = base.ReadBytes(8);
            Array.Reverse(data);
            return BitConverter.ToInt64(data, 0);
        }

        public override UInt16 ReadUInt16()
        {
            var data = base.ReadBytes(2);
            Array.Reverse(data);
            return BitConverter.ToUInt16(data, 0);
        }

        public override UInt32 ReadUInt32()
        {
            var data = base.ReadBytes(4);
            Array.Reverse(data);
            return BitConverter.ToUInt32(data, 0);
        }

        public override UInt64 ReadUInt64()
        {
            var data = base.ReadBytes(8);
            Array.Reverse(data);
            return BitConverter.ToUInt64(data, 0);
        }

        /* Custom DNS Readers */

        public String ReadPrefixedString()
        {
            int length = ReadByte();
            return ReadString(length);
        }

        public byte[] ReadPrefixedBytes()
        {
            int length = ReadByte();
            return ReadBytes(length);
        }

        public String ReadString(int length)
        {
            Byte[] bytes = ReadBytes(length);
            return System.Text.Encoding.UTF8.GetString(bytes);
        }

        /*
        private String ReadString(int offset, int length)
        {
            long oldPosition = this.ber.BaseStream.Position;
            this.ber.BaseStream.Position = offset;
            String ret = ReadString(length);
            this.ber.BaseStream.Position = oldPosition;
            return ret;
        }
        */

        /// <summary>
        /// Reads a domain name from the packet
        /// </summary>
        /// <returns>The name.</returns>
        public String ReadName()
        {
            String result = "";
            long off = base.BaseStream.Position;
            int length = 0;
            long next = -1;
            long first = off;
            int t;

            while (true)
            {
                base.BaseStream.Position = off;
                length = ReadByte();
                off += 1;
                if (length == 0)
                    break;
                t = length & 0xC0;
                if (t == 0x00)
                {
                    base.BaseStream.Position = off;
                    result = result + ReadString(length) + '.';
                    off += length;
                }
                else if (t == 0xC0)
                {
                    if (next < 0)
                        next = off + 1;
                    off = ((length & 0x3F) << 8) | PeekChar();
                    if (off >= first)
                        throw new IncomingDecodeError(
                            String.Format("Bad domain name (circular) at {0}", off)
                        );
                    first = off;
                }
                else
                    throw new IncomingDecodeError(String.Format("Bad domain name at {0}", off));
            }

            if (next >= 0)
                base.BaseStream.Position = next;
            else
                base.BaseStream.Position = off;

            return result;
        }
    }
}
