using System;
namespace Zeroconf
{
    public interface IServiceListener
    {
        void UpdateRecord(Zeroconf zc, long now, DNSRecord record);
    }
}
