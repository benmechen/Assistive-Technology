using System;
using System.IO;

namespace Zeroconf
{
    /// <summary>
    /// A DNS host information record
    /// </summary>
    public class DNSHinfo : DNSRecord
    {
        public string CPU;
        public string OS;

        public DNSHinfo(String name, DNSType type, DNSClass cls, uint ttl, String cpu, String os)
            : base(name, type, cls, ttl)
        {
            this.CPU = cpu;
            this.OS = cpu;
        }

        /// <summary>
        /// Used in constructing an outgoing packet
        /// </summary>
        /// <returns>The write.</returns>
        /// <param name="outgoing">Outgoing.</param>
        public override void Write(BigEndianWriter w)
        {   
            w.WritePrefixed(this.CPU);
            w.WritePrefixed(this.OS);
        }

        public override bool Equals(object obj)
        {
            DNSHinfo other = obj as DNSHinfo;
            return (this == other &&
                    this.CPU == other.CPU &&
                    this.OS == other.OS);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return this.CPU + " " + this.OS;
        }
    }
}
