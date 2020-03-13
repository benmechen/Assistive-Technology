using System;
using System.Net;

namespace Zeroconf
{
    /// <summary>
    /// A DNS address record
    /// </summary>
    public class DNSAddress : DNSRecord
    {
        public byte[] Address;

        public DNSAddress(String name, DNSType type, DNSClass cls, uint ttl,
                          byte[] address)
            : base(name, type, cls, ttl)
        {
            this.Address = address;
        }

        /// <summary>
        /// Used in constructing an outgoing packet
        /// </summary>
        /// <param name="outgoing">Outgoing.</param>
        public override void Write(BigEndianWriter w)
        {
            w.Write(this.Address);
        }

        public override bool Equals(object obj)
        {
            DNSAddress other = obj as DNSAddress;
            return (this == other &&
                    this.Address == other.Address);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return new IPAddress(this.Address).ToString();
        }
    }
}
