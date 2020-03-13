using System;
namespace Zeroconf
{
    [Flags]
    public enum DNSClass : ushort
    {
        IN = 1,
        CS = 2,
        CH = 3,
        HS = 4,
        NONE = 254,
        ANY = 255,
        MASK = 0x7FFF,
        UNIQUE = 0x8000
    }
}
