using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Linq;

namespace Zeroconf
{
    /// <summary>
    /// An engine wraps read access to sockets, allowing objects that
    /// need to receive data from sockets to be called back when the
    /// sockets are ready.
    ///
    /// A reader needs a handle_read() method, which is called when the socket
    /// it is interested in is ready for reading.
    ///
    /// Writers are not implemented here, because we only send short
    /// packets.
    /// </summary>
    public class Engine
    {
        private Thread t;
        private Zeroconf zc;
        private Dictionary<Socket, IReader> readers;
        private int timeout;
        private object condition;

        public Engine(Zeroconf zc)
        {
            this.t = new Thread(Run)
            {
                Name = "zeroconf-Engine",
                IsBackground = true
            };
            this.zc = zc;
            this.readers = new Dictionary<Socket, IReader>();
            this.timeout = 5;
            this.condition = new object();
            this.t.Start();
        }

        public void Run()
        {
            while (!this.zc.Done)
            {
                List<Socket> rs = new List<Socket>();
                lock (this.condition)
                {
                    rs = this.readers.Keys.ToList();
                    if (rs.Count == 0)
                    {
                        // No sockets to manage, but we wait for the timeout
                        // or addition of a socket
                        Monitor.Wait(this.condition, this.timeout * 1000);
                    }
                }

                if (rs.Count != 0)
                {
                    try
                    {
                        // Socket.Select removes stale sockets
                        // Source: https://msdn.microsoft.com/en-us/library/system.net.sockets.socket.select(v=vs.110).aspx 
                        Socket.Select(rs, null, null, this.timeout);

                        if (this.zc.Done)
                            continue;
                        
                        foreach (Socket socket in rs)
                        {
                            if (this.readers.ContainsKey(socket))
                            {
                                IReader r = this.readers[socket];
                                r.HandleRead(socket);
                            }
                        }
                    }
                    catch (SocketException e)
                    {
                        // If the socket was closed by another thread, during
                        // shutdown, ignore it and exit
                        // e.args[0].EBADF ?eq? SocketError.SocketError
                        if (e.SocketErrorCode != SocketError.NotConnected && !this.zc.Done)
                            throw;
                    }
                }
            }
        }

        public void Join()
        {
            this.t.Join();
        }

        public void AddReader(IReader reader, Socket socket)
        {
            lock (this.condition)
            {
                this.readers.Add(socket, reader);
                Monitor.Pulse(this.condition);
            }
        }

        public void DeleteReader(Socket socket)
        {
            lock (this.condition)
            {
                this.readers.Remove(socket);
                Monitor.Pulse(this.condition);
            }
        }
    }
}
