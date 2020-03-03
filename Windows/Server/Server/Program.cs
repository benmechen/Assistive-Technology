using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using ArkaneSystems.Arkane.Zeroconf;

namespace Zeroconf
{

    class MainClass
    {
        // https://msdn.microsoft.com/fr-fr/library/windows/desktop/ms686016.aspx
        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(SetConsoleCtrlEventHandler handler, bool add);

        // https://msdn.microsoft.com/fr-fr/library/windows/desktop/ms683242.aspx
        private delegate bool SetConsoleCtrlEventHandler(CtrlType sig);

        readonly UdpClient receiver = new UdpClient(1024);
        IPEndPoint sender = new IPEndPoint(IPAddress.Any, 1024);
        static readonly MainClass p = new MainClass();
        static readonly RegisterService service = new RegisterService();

        private enum CtrlType
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }

        public void Send(string message, UdpClient udp, IPEndPoint end)
        {
            byte[] messageByte = System.Text.Encoding.UTF8.GetBytes(message);
            var byteSent = udp.Send(messageByte, messageByte.Length, end);
            Console.WriteLine("Sent to client: bytes: {0} - {1}", byteSent, message);
        }
        public static void Main(string[] args)
        {

            SetConsoleCtrlHandler(Handler, true);

            Console.WriteLine("Sample service publisher using arkane.Mono.Zeroconf version\n");

            string hostName = Dns.GetHostName();
            string ipaddress = Dns.GetHostEntry(hostName).AddressList[0].ToString();
            
        
            
            service.Name = "Assistive Technology Server";
            service.RegType = "_assistive-tech._udp";
            service.ReplyDomain = "local.";
            service.Port = 1024;


            TxtRecord txt_record = new TxtRecord
            {
                { "service", "Assistive Technology Technology" },
                { "version", "1.0.0" }
            };
            service.TxtRecord = txt_record;

            try
            {
                service.Register();
                Console.WriteLine("{0} service has been registered", service.Name);

            }
            catch (Exception ex)
            {
                Console.WriteLine("Service Error: {0}", ex.ToString());
            }
           
           
            while (true)
            {                              
                byte[] rmessage = p.receiver.Receive(ref p.sender);
                string smessage = System.Text.Encoding.UTF8.GetString(rmessage);
                Console.WriteLine("Received:" + smessage + " " + p.sender.Address.ToString());
                p.Send("astv_ack", p.receiver, p.sender);
                if (smessage == "astv_discover")
                {
                    Console.WriteLine("Discover call from client: " + p.sender.Address.ToString());
                    p.Send("astv_shake:" + ipaddress, p.receiver, p.sender);
                    Console.WriteLine("Sent handshake: astv_shake to address:" + p.sender.Address.ToString());
                }
                else if (smessage == "astv_disconnect") break;             
            }
            
        }

        private static void ShutDown()
        {
            Console.WriteLine("\n Shutting down server [" + DateTime.Now + "]");
            if (p.sender.Address != null)
                p.Send("astv_disconnect", p.receiver, p.sender);
            service.Dispose();
            p.receiver.Close();
            Environment.Exit(0);
        }

        private static bool Handler(CtrlType signal)
        {
            switch (signal)
            {
                case CtrlType.CTRL_BREAK_EVENT:
                    ShutDown();
                    return false;
                case CtrlType.CTRL_C_EVENT:
                case CtrlType.CTRL_LOGOFF_EVENT:
                case CtrlType.CTRL_SHUTDOWN_EVENT:
                    ShutDown();
                    return false;
                case CtrlType.CTRL_CLOSE_EVENT:
                    ShutDown();
                    return false;

                default:
                    return false;
            }
        }
    }
}
