using System;
namespace Zeroconf
{
    /// <summary>
    /// A DNS entry
    /// </summary>
    public class DNSEntry
    {
        public string Key;
        public string Name;
        public DNSType Type;
        public DNSClass Class;
        public bool Unique;

        public DNSEntry(string name, DNSType type, DNSClass cls)
        {
            this.Key = name.ToLower();
            this.Name = name;
            this.Type = type;
            this.Class = cls & DNSClass.MASK;
            this.Unique = (cls & DNSClass.UNIQUE) != 0;
        }

        public override bool Equals(Object obj)
        {
            DNSEntry other = obj as DNSEntry;
            return (this.Name == other.Name &&
                    this.Type == other.Type &&
                    this.Class == other.Class);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static string GetClass(DNSClass cls)
        {
            string ret;
            if (!Mapping.ClassType.TryGetValue(cls, out ret))
                ret = String.Format("?({0})", (int)cls);

            return ret;
        }

        public static string GetType(DNSType type)
        {
            string ret;
            if (!Mapping.DnsType.TryGetValue(type, out ret))
                ret = String.Format("?({0})", (int)type);

            return ret;
        }

        public string ToString(string header, string other = null)
        {
            string result = String.Format("{0}[{1},{2}",
                                          header,
                                          DNSEntry.GetType(this.Type),
                                          DNSEntry.GetClass(this.Class));
            if (this.Unique)
                result += "-unique,";
            else
                result += ",";

            result += this.Name;

            if (other != null)
                result += String.Format(",{0}]", other);
            else
                result += "]";
            return result;
        }
    }
}
