using System;
using System.Net;

namespace Zeroconf
{
    /// <summary>
    /// A DNS service record
    /// </summary>
    public class DNSService : DNSRecord
    {
        public ushort Priority;
        public ushort Weight;
        public ushort Port;
        public string Server;

        public DNSService(String name, DNSType type, DNSClass cls, uint ttl,
                          ushort priority, ushort weight, ushort port, string server)
            : base(name, type, cls, ttl)
        {
            this.Priority = priority;
            this.Weight = weight;
            this.Port = port;
            this.Server = server;
        }

        /// <summary>
        /// Used in constructing an outgoing packet
        /// </summary>
        /// <returns>The write.</returns>
        /// <param name="outgoing">Outgoing.</param>
        public override void Write(BigEndianWriter w)
        {
            w.Write(this.Priority);
            w.Write(this.Weight);
            w.Write(this.Port);
            w.WriteName(this.Server);
        }

        public override bool Equals(object obj)
        {
            DNSService other = obj as DNSService;
            return (this == other &&
                   this.Priority == other.Priority &&
                   this.Weight == other.Weight &&
                   this.Port == other.Port &&
                   this.Server == other.Server);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return base.ToString(String.Format("{0}:{1}", this.Server.ToString(), this.Port));
        }
    }
}
