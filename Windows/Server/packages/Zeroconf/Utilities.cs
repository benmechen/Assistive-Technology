using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Zeroconf
{
    public class Utilities
    {
        /// <summary>
        /// Get current system time in milliseconds
        /// </summary>
        /// <returns>The time milliseconds.</returns>
        public static long CurrentTimeMilliseconds()
        {
            DateTime dt = DateTime.Now;
            return new DateTimeOffset(dt).ToUnixTimeMilliseconds();
        }

        public static string AddressToString(byte[] addr)
        {
            return new IPAddress(addr).ToString();
        }

        public static byte[] AddressToBytes(string addr)
        {
            return IPAddress.Parse(addr).GetAddressBytes();
        }

        /// <summary>
        /// Validate a fully qualified service name, instance or subtype. [rfc6763]
        ///
        /// Returns fully qualified service name.
        ///
        /// Domain names used by mDNS-SD take the following forms:
        ///
        ///               <sn> . <_tcp|_udp> . local.
        ///  <Instance> . <sn> . <_tcp|_udp> . local.
        ///   <sub>._sub. <sn> . <_tcp|_udp> . local.
        ///
        /// 1) must end with 'local.'
        ///
        /// This is true because we are implementing mDNS and since the 'm' means
        /// multi-cast, the 'local.' domain is mandatory.
        ///
        /// 2) local is preceded with either '_udp.' or '_tcp.'
        ///
        /// 3) service name<sn> precedes<_tcp|_udp>
        ///
        /// The rules for Service Names [RFC6335] state that they may be no more
        /// than fifteen characters long (not counting the mandatory underscore),
        /// consisting of only letters, digits, and hyphens, must begin and end
        /// with a letter or digit, must not contain consecutive hyphens, and
        /// must contain at least one letter.
        ///
        /// The instance name<Instance> and sub type<sub> may be up to 63 bytes.
        ///
        /// The portion of the Service Instance Name is a user-
        ///
        /// friendly name consisting of arbitrary Net-Unicode text [RFC5198]. It
        /// MUST NOT contain ASCII control characters (byte values 0x00-0x1F and
        /// 0x7F) [RFC20] but otherwise is allowed to contain any characters,
        /// without restriction, including spaces, uppercase, lowercase,
        /// punctuation -- including dots -- accented characters, non-Roman text,
        /// and anything else that may be represented using Net-Unicode.
        /// </summary>
        /// <returns>Fully qualified service name (eg: _http._tcp.local.)</returns>
        /// <param name="type">Type, SubType or service name to validate</param>
        public static string ServiceTypeName(string type)
        {
            if (!type.EndsWith("._tcp.local.", StringComparison.CurrentCulture) && !type.EndsWith("._udp.local.", StringComparison.CurrentCulture))
                throw new BadTypeInNameException(String.Format("Type {0} must end with \"._tcp.local.\" or \"._udp.local.\"", type));

            Stack<string> remaining = new Stack<string>(type.Substring(0, type.Length - "._tcp.local.".Length).Split('.'));
            string name = remaining.Pop();

            if (String.IsNullOrEmpty(name))
                throw new BadTypeInNameException("No Service name found");

            if (remaining.Count == 1 && remaining.Peek().Length == 0)
                throw new BadTypeInNameException(String.Format("Type \"{0}\" must not start with \'.\'", type));

            if (!name.StartsWith("_", StringComparison.CurrentCulture))
                throw new BadTypeInNameException(String.Format("Service name ({0}) must start with \'_\'", name));

            // Remove leading underscore
            name = name.Substring(1);

            if (name.Length > 15)
                throw new BadTypeInNameException(String.Format("Service name ({0}) must be <= 15 bytes", name));

            if (name.Contains("--"))
                throw new BadTypeInNameException(String.Format("Service name ({0}) must not contain \'--\'", name));

            if (name.StartsWith("-", StringComparison.CurrentCulture) || name.EndsWith("-", StringComparison.CurrentCulture))
                throw new BadTypeInNameException(String.Format("Service name ({0}) must not start or end with \'-\'", name));

            if (!Regexes.HAS_A_TO_Z.IsMatch(name))
                throw new BadTypeInNameException(String.Format("Service name ({0}) must contain at least one letter (eg: \'A-Z\')", name));

            if (!Regexes.HAS_ONLY_A_TO_Z_NUM_HYPHEN.IsMatch(name))
                throw new BadTypeInNameException(String.Format("Service name ({0}) must contain only these characters: A-Z, a-z, 0-9, hyphen ('-')", name));

            if (remaining.Count > 0 && remaining.Contains("_sub"))
            {
                remaining.Pop();
                if (remaining.Count == 0 || remaining.Peek().Length == 0)
                    throw new BadTypeInNameException("_sub requires a subtype name");
            }

            string remaining_string = "";
            if (remaining.Count > 0)
                remaining_string = String.Join(".", remaining);

            if (!String.IsNullOrEmpty(remaining_string))
            {
                if (remaining_string.Length > 63)
                    throw new BadTypeInNameException(String.Format("Too long: \'{0}\'", remaining_string));

                if (Regexes.HAS_ASCII_CONTROL_CHARS.IsMatch(remaining_string))
                    throw new BadTypeInNameException(String.Format("ASCII control character 0x00-0x1F and 0x7F illegal in \'{0}\'", remaining_string));
            }

            return "_" + name + type.Substring(type.Length - "._tcp.local.".Length);
        }

        static public List<IPInterfaceProperties> GetAllAddresses(AddressFamily addressFamily)
        {
            if (addressFamily != AddressFamily.InterNetwork &&
                addressFamily != AddressFamily.InterNetworkV6)
                throw new InvalidOperationException("AddressFamily either IPv4 or IPv6");

            List<IPInterfaceProperties> result = new List<IPInterfaceProperties>();

            NetworkInterface[] ifs = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface netInterface in ifs)
            {
                IPInterfaceProperties ipProps = netInterface.GetIPProperties();
                // if (netInterface.GetIPProperties().MulticastAddresses.Count == 0)
                //    continue; // most of VPN adapters will be skipped
                if (!netInterface.SupportsMulticast)
                    continue; // multicast is meaningless for this type of connection
                if (OperationalStatus.Up != netInterface.OperationalStatus)
                    continue; // this adapter is off or not connected
                if ((addressFamily == AddressFamily.InterNetwork &&
                     ipProps.GetIPv4Properties() == null) ||
                    (addressFamily == AddressFamily.InterNetworkV6 &&
                     ipProps.GetIPv6Properties() == null))
                {
                    continue; // IPv4 is not configured on this adapter
                }
                result.Add(ipProps);
            }
            return result;
        }

        static public List<IPAddress> NormalizeInterfaceChoice(InterfaceChoice choice, AddressFamily addressFamily)
        {
            List<IPAddress> list = new List<IPAddress>();
            if (choice == InterfaceChoice.Default)
                list.Add(IPAddress.Parse("0.0.0.0"));
            else
            {
                List<IPInterfaceProperties> tmp = GetAllAddresses(addressFamily);
                foreach (IPInterfaceProperties props in tmp)
                {
                    foreach (IPAddressInformation info in props.AnycastAddresses)
                    {
                        if (info.Address.AddressFamily == addressFamily &&
                            info.Address.ToString() != IPAddress.Parse("127.0.0.1").ToString())
                            list.Add(info.Address);
                    }
                }

            }
            return list;
        }
    }
}
