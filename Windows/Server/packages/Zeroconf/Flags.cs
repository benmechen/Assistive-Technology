using System;
namespace Zeroconf
{
    public enum QueryFlags : ushort
    {
        Mask = 0x8000, // Query Response Mask
        Query = 0x0000,
        Response = 0x8000
    }

    public enum Flags : ushort
    {
        AA = 0x0400, // Authoritative answer
        TC  = 0x0200, // Truncated
        RD  = 0x0100, // Recursion desired
        RA  = 0x8000, // Recursion available

        Z   = 0x0040, // ZERO
        AD  = 0x0020, // Authentic data
        CD  = 0x0010 // Checking disabled
    }
}
