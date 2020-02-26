using System;
using System.Net;

namespace Zeroconf
{
    public class Constants
    {
        public const int MDNS_PORT = 5353;
        public const int DNS_PORT = 53;
        public const int DNS_TTL = 60 * 60; // one hour default
        public const int MAX_MSG_TYPICAL = 1460;
        public const int MAX_MSG_ABSOLUTE = 8966;

        public static IPAddress MDNS_ADDRESS = IPAddress.Parse("224.0.0.251");
        public static IPAddress HOST_ONLY_NETWORK_MASK = IPAddress.Parse("255.255.255.255");

    }
}
