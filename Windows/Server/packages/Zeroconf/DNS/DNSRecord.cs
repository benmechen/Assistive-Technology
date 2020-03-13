using System;
namespace Zeroconf
{
    /// <summary>
    /// A DNS record - like a DNS entry, but has a TTL
    /// </summary>
    public abstract class DNSRecord : DNSEntry
    {
        public uint TTL;
        public long Created;
        public DNSRecord(String name, DNSType type, DNSClass cls, uint ttl) : base(name, type, cls)
        {
            this.TTL = ttl;
            this.Created = Utilities.CurrentTimeMilliseconds();
        }

        public override bool Equals(Object obj)
        {
            throw new AbstractMethodException("");
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        /// Returns true if any answer in a message can suffice for the
        /// information held in this record
        /// </summary>
        /// <returns><c>true</c>, if supressed, <c>false</c> otherwise.</returns>
        /// <param name="incoming">Incoming.</param>
        public bool SuppressedBy(DNSIncoming incoming)
        {
            foreach(DNSRecord record in incoming.Answers)
            {
                if (SuppressedByAnswer(record))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Returns true if another record has same name, type and class,
        /// and if its TTL is at least half of this record's
        /// </summary>
        /// <returns><c>true</c>, if by answer was suppresseded, <c>false</c> otherwise.</returns>
        /// <param name="other">Other.</param>
        public bool SuppressedByAnswer(DNSRecord other)
        {
            return (this == other &&
                    other.TTL > (this.TTL / 2));
        }

        /// <summary>
        /// Returns the time at which this record will have expired
        /// by a certain percentage.
        /// </summary>
        /// <returns>The expiration time.</returns>
        /// <param name="percent">Percent.</param>
        public long GetExpirationTime(long percent)
        {
            return this.Created + (percent * this.TTL * 10);
        }

        /// <summary>
        /// Returns the remaining TTL in seconds.
        /// </summary>
        /// <returns>The remaining ttl.</returns>
        /// <param name="now">Now.</param>
        public long GetRemainingTTL(long now)
        {
            return Math.Max(0, this.GetExpirationTime(100) - now);
        }

        /// <summary>
        /// Returns true if this record has expired.
        /// </summary>
        /// <returns><c>true</c>, if record is expired, <c>false</c> otherwise.</returns>
        /// <param name="now">Now.</param>
        public bool IsExpired(long now)
        {
            return this.GetExpirationTime(100) <= now;
        }

        /// <summary>
        /// Returns true if this record is at least half way expired.
        /// </summary>
        /// <returns><c>true</c>, if record is stale, <c>false</c> otherwise.</returns>
        /// <param name="now">Now.</param>
        public bool IsStale(long now)
        {
            return this.GetExpirationTime(50) <= now;
        }

        /// <summary>
        /// Sets this record's TTL and created time to that of
        /// another record
        /// </summary>
        /// <param name="other">Other.</param>
        public void ResetTTL(DNSRecord other)
        {
            this.Created = other.Created;
            this.TTL = other.TTL;
        }

        public abstract void Write(BigEndianWriter w);

        public string ToString(DNSRecord other)
        {
            long remainingTime = this.GetRemainingTTL(Utilities.CurrentTimeMilliseconds());
            String arg = String.Format("{0}/{1},{2}",
                                       this.TTL,
                                       remainingTime,
                                       other);
            return base.ToString("record", arg);
        }
    }
}
