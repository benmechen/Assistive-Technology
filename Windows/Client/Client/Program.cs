using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zeroconf;
using System.Net;
using System.Net.Sockets;

namespace Client
{
    class NetworkServices
    {
        public async Task ProbeForServices()
        {
            IReadOnlyList<IZeroconfHost> results = await
                ZeroconfResolver.ResolveAsync("_assistive-tech._udp.local.");
            foreach (var resp in results)
                Console.WriteLine(resp);

        }

        public async Task EnumerateAllServicesFromAllHosts()
        {
            ILookup<string, string> domains = await ZeroconfResolver.BrowseDomainsAsync();
            var responses = await ZeroconfResolver.ResolveAsync(domains.Select(g => g.Key));

            foreach (var resp in responses)
                Console.WriteLine(resp);

            Console.WriteLine("Done!");
        }
    }
    class Program
    {
        static async Task Main(string[] args)
        {

            Console.WriteLine("Client!");
            NetworkServices p = new NetworkServices();

            Console.WriteLine("Probing for assistive tech services: \n");
            await p.ProbeForServices();
            Console.WriteLine("General Probing: ");
            await p.EnumerateAllServicesFromAllHosts();
            Console.ReadLine();
        }
    }
}
