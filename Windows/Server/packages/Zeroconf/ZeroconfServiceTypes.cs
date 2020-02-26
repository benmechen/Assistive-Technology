using System;
using System.Collections.Generic;

namespace Zeroconf
{
    public class ZeroconfServiceTypes : IListener
    {
        private List<string> foundServices;
        private ZeroconfServiceTypes()
        {
            this.foundServices = new List<string>();
        }

        public void AddService(Zeroconf zc, string type, string name)
        {
            this.foundServices.Add(name);
        }

        public void RemoveService(Zeroconf zc, string type, string name)
        {
            
        }

        static public List<string> Find(Zeroconf zc=null, int timeout=5,
                                InterfaceChoice interfaces=InterfaceChoice.All)
        {
            Zeroconf localZc = null;
            if (zc != null)
                localZc = zc;
            else
                localZc = new Zeroconf(interfaces: interfaces);
            
            ZeroconfServiceTypes listener = new ZeroconfServiceTypes();
            ServiceBrowser browser = new ServiceBrowser(
                localZc, "_services._dns-sd._udp.local.",
                listener: listener
            );

            // Wait for responses
            System.Threading.Thread.Sleep(timeout * 1000);

            // Close down anything we opened
            if (zc == null)
                localZc.Close();
            else
                browser.Cancel();

            return listener.foundServices;
        }
    }
}
