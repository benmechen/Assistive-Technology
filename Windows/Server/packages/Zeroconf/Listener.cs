using System;
using System.Net;
using System.Net.Sockets;

namespace Zeroconf
{
    public class Listener : IReader
    {
        private Zeroconf zc;
        private byte[] data;

        public Listener(Zeroconf zc)
        {
            this.zc = zc;
        }

        public void HandleRead(Socket socket)
        {
            int recvSize = 0;
            byte[] buf = new byte[Constants.MAX_MSG_ABSOLUTE];
            EndPoint remoteEp = new IPEndPoint(0, 0);

            try
            {
                recvSize = socket.ReceiveFrom(buf, ref remoteEp);
            }
            catch(SocketException e)
            {
                Console.WriteLine("HandleRead: Threw {0}", e);
                return;
            }

            Console.WriteLine("Received {0} bytes from {1}", recvSize, remoteEp);
            Array.Resize<byte>(ref buf, recvSize);

            this.data = buf;
            DNSIncoming msg = new DNSIncoming(buf);
            if (!msg.Valid)
            {
                Console.WriteLine("Incoming msg not valid...");
            }
            else if (msg.IsQuery)
            {
                // Always multicast responses
                if (((IPEndPoint)remoteEp).Port == Constants.MDNS_PORT)
                    this.zc.HandleQuery(msg, Constants.MDNS_ADDRESS, Constants.MDNS_PORT);
                // If it's not a multicast query, reply via unicast
                // and multicast
                else if (((IPEndPoint)remoteEp).Port == Constants.DNS_PORT)
                {
                    this.zc.HandleQuery(msg, ((IPEndPoint)remoteEp).Address, ((IPEndPoint)remoteEp).Port);
                    this.zc.HandleQuery(msg, Constants.MDNS_ADDRESS, Constants.MDNS_PORT);
                }
            }
            else
            {
                this.zc.HandleResponse(msg);
            }
        }
    }
}
