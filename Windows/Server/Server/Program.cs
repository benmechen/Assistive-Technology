using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Zeroconf;

namespace Zeroconf
{
    class MyListener : IListener
    {
        public void AddService(Zeroconf zc, string type, string name)
        {
            ServiceInfo info = zc.GetServiceInfo(type.ToString(), name);
            System.Console.WriteLine("Service {0} added, info: {1}", name, info);
            
        }

        public void RemoveService(Zeroconf zc, string type, string name)
        {
            ServiceInfo info = zc.GetServiceInfo(type, name);
            System.Console.WriteLine("Service removed :: {0}", info);
        }
    }

    class MainClass
    {
        public static void Main(string[] args)
        {
            string serviceName = " Assistive Technology Server._assistive-tech._udp.local.";
            string type = "_assistive-tech._udp.local.";
            System.Console.WriteLine("Zeroconf Sample Application");
            Zeroconf zeroconf = new Zeroconf();                                  
            Dictionary<string, string> properties =
                new Dictionary<string, string>();
            properties.Add("service", "Assistive Technology Service");
            properties.Add("version", "1.0.0");
            string hostName = Dns.GetHostName(); // Retrive the Name of HOST  
            IPAddress myIP = Dns.GetHostEntry(hostName).AddressList[0];
            Console.WriteLine("My IP address {0}", myIP);
            ServiceInfo f = new ServiceInfo(type, hostName + serviceName, myIP.ToString(), 1024, 0, 0, properties, hostName + ".local.");
            zeroconf.RegisterService(f);

            zeroconf.UnregisterAllServices();
            System.Console.WriteLine("Service Registered {0}\n", f);
            Console.ReadLine();

            System.Console.WriteLine("Bye!");
            zeroconf.UnregisterService(f);
            ServiceInfo l = zeroconf.GetServiceInfo(type, hostName + serviceName);
            Console.WriteLine("UnregisteredService {0}", l);
            zeroconf.Close();
        }
    }
}
