using System;
using System.Collections.Generic;

namespace Zeroconf
{
    public class Mapping
    {
        public static Dictionary<DNSType, string> DnsType = new Dictionary<DNSType, string>()
        {
            {DNSType.A, "a"},
            {DNSType.NS, "ns"},
            {DNSType.MD, "md"},
            {DNSType.MF, "mf"},
            {DNSType.CNAME, "cname"},
            {DNSType.SOA, "soa"},
            {DNSType.MB, "mb"},
            {DNSType.MG, "mg"},
            {DNSType.MR, "mr"},
            {DNSType.NULL, "null"},
            {DNSType.WKS, "wks"},
            {DNSType.PTR, "ptr"},
            {DNSType.HINFO, "hinfo"},
            {DNSType.MINFO, "minfo"},
            {DNSType.MX, "mx"},
            {DNSType.TXT, "txt"},
            {DNSType.AAAA, "quada"},
            {DNSType.SRV, "srv"},
            {DNSType.ANY, "any"}
        };

        public static Dictionary<DNSClass, string> ClassType = new Dictionary<DNSClass, string>()
        {
            {DNSClass.IN, "in"},
            {DNSClass.CS, "cs"},
            {DNSClass.CH, "ch"},
            {DNSClass.HS, "hs"},
            {DNSClass.NONE, "none"},
            {DNSClass.ANY, "any"}
        };
    }
}
