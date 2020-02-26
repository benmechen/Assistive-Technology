using System;
namespace Zeroconf
{
    /// <summary>
    /// A DNS question entry
    /// </summary>
    public class DNSQuestion : DNSEntry
    {
        public DNSQuestion(String name, DNSType type, DNSClass cls) : base(name, type, cls)
        {
        }

        /// <summary>
        /// Check wether question is answered by record
        /// </summary>
        /// <returns>Returns <c>true</c> if question is answered by record,
        /// <c>false</c> otherwise/</returns>
        /// <param name="record">Record.</param>
        public bool AnsweredBy(DNSRecord record)
        {
            return (base.Class == record.Class &&
                    (base.Type == record.Type || base.Type == DNSType.ANY) &&
                    base.Name == record.Name);
        }

        public override string ToString()
        {
            return base.ToString("question", null);
        }
    }
}
