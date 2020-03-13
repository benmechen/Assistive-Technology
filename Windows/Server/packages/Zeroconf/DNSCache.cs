using System;
using System.Linq;
using System.Collections.Generic;

namespace Zeroconf
{
    /// <summary>
    /// A cache of DNS entries
    /// </summary>
    public class DNSCache
    {
        private Dictionary<string, List<DNSEntry>> cache;

        public DNSCache()
        {
            this.cache = new Dictionary<string, List<DNSEntry>>();
        }

        /// <summary>
        /// Adds an entry
        /// </summary>
        /// <param name="entry">DNS Record to add.</param>
        public void Add(DNSEntry entry)
        {
            List<DNSEntry> records;
            bool success = this.cache.TryGetValue(entry.Key, out records);
            if (!success)
            {
                records = new List<DNSEntry>();
            }
            records.Insert(0, entry);
            this.cache[entry.Key] = records;
        }

        /// <summary>
        /// Removes an entry
        /// </summary>
        /// <param name="entry">Entry.</param>
        public void Remove(DNSEntry entry)
        {
            if(this.cache.ContainsKey(entry.Key))
            {
                List<DNSEntry> records = this.cache[entry.Key];
                records.Remove(entry);
            }
        }

        /// <summary>
        /// Gets an entry by key.
        /// </summary>
        /// <returns><c>DNSEntry</c> if entry is found, <c>null</c> otherwise</returns>
        /// <param name="entry">Entry.</param>
        public DNSEntry Get(DNSEntry entry)
        {
            if (this.cache.ContainsKey(entry.Key))
            {
                List<DNSEntry> records = this.cache[entry.Key];
                foreach(DNSEntry cachedEntry in records)
                {
                    if (entry.Equals(cachedEntry))
                        return cachedEntry;
                }
            }
            return null;
        }

        /// <summary>
        /// Gets an entry by details
        /// </summary>
        /// <returns><c>DNSEntry</c> if entry is found, <c>null</c> otherwise</returns>
        /// <param name="name">Name.</param>
        /// <param name="type">Type.</param>
        /// <param name="cls">Cls.</param>
        public DNSEntry GetByDetails(string name, DNSType type, DNSClass cls)
        {
            DNSEntry entry = new DNSEntry(name, type, cls);
            return Get(entry);
        }

        /// <summary>
        /// Returns a list of entries whose key matches the name.
        /// </summary>
        /// <returns>A list of DNSEntries</returns>
        /// <param name="name">Name.</param>
        public List<DNSEntry> EntriesWithName(string name)
        {
            string searchName = name.ToLower();
            if(this.cache.ContainsKey(searchName))
            {
                return this.cache[searchName];
            }
            return new List<DNSEntry>();
        }

        public DNSRecord CurrentEntryWithNameAndAlias(string name, string alias)
        {
            long now = Utilities.CurrentTimeMilliseconds();
            foreach(DNSRecord record in EntriesWithName(name))
            {
                if (record.Type == DNSType.PTR &&
                    !record.IsExpired(now) &&
                    ((DNSPointer)record).Alias == alias)
                {
                    return record;
                }
            }
            return null;
        }

        /// <summary>
        /// Returns a list of all entries
        /// </summary>
        /// <returns>The entries.</returns>
        public List<DNSEntry> Entries()
        {
            if (this.cache.Count == 0)
                return new List<DNSEntry>();

            return this.cache.Values.SelectMany(x => x).ToList();
        }
    }
}
