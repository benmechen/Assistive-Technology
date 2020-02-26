using System;
namespace Zeroconf
{
    public interface IListener
    {
        void AddService(Zeroconf zc, string type, string name);
        void RemoveService(Zeroconf zc, string type, string name);
    }
}
