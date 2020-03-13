using System;
namespace Zeroconf
{
    /// <summary>
    /// A DNS pointer record
    /// </summary>
    public class DNSPointer : DNSRecord
    {
        public String Alias; 

        public DNSPointer(String name, DNSType type, DNSClass cls, uint ttl, String alias)
            : base(name, type, cls, ttl)
        {
            this.Alias = alias;
        }

        /// <summary>
        /// Used in constructing an outgoing packet
        /// </summary>
        /// <returns>The write.</returns>
        /// <param name="outgoing">Outgoing.</param>
        public override void Write(BigEndianWriter w)
        {
            w.WriteName(this.Alias);
        }

        public override bool Equals(object obj)
        {
            DNSPointer other = obj as DNSPointer;
            return (this == other &&
                    this.Alias == other.Alias);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return base.ToString(this.Alias);
        }
    }
}
