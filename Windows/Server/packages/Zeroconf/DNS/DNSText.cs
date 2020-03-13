using System;
namespace Zeroconf
{
    /// <summary>
    /// A DNS text record
    /// </summary>
    public class DNSText : DNSRecord
    {
        public byte[] Text;

        public DNSText(String name, DNSType type, DNSClass cls, uint ttl, byte[] text)
            : base(name, type, cls, ttl)
        {
            this.Text = text;
        }

        /// <summary>
        /// Used in constructing an outgoing packet
        /// </summary>
        /// <returns>The write.</returns>
        /// <param name="outgoing">Outgoing.</param>
        public override void Write(BigEndianWriter w)
        {
            w.Write(this.Text);
        }

        public override bool Equals(object obj)
        {
            DNSText other = obj as DNSText;
            return (this == other &&
                    this.Text == other.Text);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            string text = System.Text.Encoding.UTF8.GetString(this.Text);
            return base.ToString(text);
        }
    }
}
