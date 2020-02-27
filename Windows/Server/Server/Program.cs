using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using ArkaneSystems.Arkane.Zeroconf;

namespace Zeroconf
{  

    class MainClass
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Sample service publisher using arkane.Mono.Zeroconf version\n");

            RegisterService service = new RegisterService();
            service.Name = "Assistive Technology Server";
            service.RegType = "_assistive-tech._udp";
            service.ReplyDomain = "local.";
            service.Port = 1024;


            TxtRecord txt_record = new TxtRecord();
            txt_record.Add("service", "Assistive Technology Technology");
            txt_record.Add("version", "1.0.0");
            service.TxtRecord = txt_record;
            service.Register();
            Console.WriteLine("Server properties have been registered");
            Console.ReadLine();
            service.Dispose();
            Console.WriteLine("service has been disposed! Bye!!!!\n");
        }
    }
}
