using System;

namespace Zeroconf
{
    public interface IReader
    {
        void HandleRead(System.Net.Sockets.Socket socket);
    }
}
