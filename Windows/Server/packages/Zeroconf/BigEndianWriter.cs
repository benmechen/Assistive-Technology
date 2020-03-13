using System;
using System.Collections.Generic;
using System.IO;

namespace Zeroconf
{
    public class BigEndianWriter : BinaryWriter
    {
        private Dictionary<String, int> Names;

        public BigEndianWriter(Stream stream) : base(stream)
        {
            this.Names = new Dictionary<string, int>();
        }

        public BigEndianWriter(byte[] buffer) : base(new MemoryStream(buffer))
        {
            this.Names = new Dictionary<string, int>();
        }

        public override void Write(Int16 value)
        {
            byte[] data = BitConverter.GetBytes(value);
            Array.Reverse(data);
            base.Write(data, 0, data.Length);
        }

        public override void Write(Int32 value)
        {
            byte[] data = BitConverter.GetBytes(value);
            Array.Reverse(data);
            base.Write(data, 0, data.Length);
        }

        public override void Write(Int64 value)
        {
            byte[] data = BitConverter.GetBytes(value);
            Array.Reverse(data);
            base.Write(data, 0, data.Length);
        }

        public override void Write(UInt16 value)
        {
            byte[] data = BitConverter.GetBytes(value);
            Array.Reverse(data);
            base.Write(data, 0, data.Length);
        }

        public override void Write(UInt32 value)
        {
            byte[] data = BitConverter.GetBytes(value);
            Array.Reverse(data);
            base.Write(data, 0, data.Length);
        }

        public override void Write(UInt64 value)
        {
            byte[] data = BitConverter.GetBytes(value);
            Array.Reverse(data);
            base.Write(data, 0, data.Length);
        }

        /* Custom DNS Writers */
        //public void Write(String str)
        //{
        //    byte[] bytes = System.Text.Encoding.UTF8.GetBytes(str);
        //    Write(bytes);
        //}

        public void WritePrefixed(String str)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(str);
            if (bytes.Length > 64)
                throw new NamePartTooLongException(
                    String.Format("WritePrefixed: Name \'{0}\'exceeding 64 bytes", str));

            Write((byte)bytes.Length);
            Write(bytes);
        }

        public void WritePrefixed(byte[] bytes)
        {
            if (bytes.Length > 256)
                throw new NamePartTooLongException(
                    String.Format("WritePrefixed: Bytestring exceeding 256 bytes"));
            Write((byte)bytes.Length);
            Write(bytes);
        }

        /// <summary>
        /// Writes names to packet
        /// 
        /// 18.14. Name Compression
        ///
        /// When generating Multicast DNS messages, implementations SHOULD use
        /// name compression wherever possible to compress the names of resource
        /// records, by replacing some or all of the resource record name with a
        /// compact two-byte reference to an appearance of that data somewhere
        /// earlier in the message[RFC1035].
        /// </summary>
        /// <param name="name">Name.</param>
        public void WriteName(string name)
        {
            WriteName(name, useCompression: true);
        }

        public void WriteName(string name, bool useCompression)
        {
            while (true)
            {
                int n = name.IndexOf('.');
                if (n < 0)
                {
                    n = name.Length;
                }
                if (n <= 0)
                {
                    Write((byte)0);
                    return;
                }
                string label = name.Substring(0, n);
                if (useCompression)
                {
                    if (this.Names.ContainsKey(name))
                    {
                        int val = this.Names[name];
                        Write((byte)((val >> 8) | 0xC0));
                        Write((byte)(val & 0xFF));
                        return;
                    }
                    // FIXME: Is this the correct offset to store?
                    this.Names.Add(name, (int)base.BaseStream.Position);
                    WritePrefixed(label);
                }
                else
                {
                    WritePrefixed(label);
                }
                name = name.Substring(n);
                if (name.StartsWith(".", StringComparison.CurrentCulture))
                {
                    name = name.Substring(1);
                }
            }
        }
    }
}
