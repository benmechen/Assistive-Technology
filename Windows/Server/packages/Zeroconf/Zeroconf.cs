using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Zeroconf
{
    /// <summary>
    /// Implementation of Zeroconf Multicast DNS Service Discovery
    /// 
    /// Supports registration, unregistration, queries and browsing.
    /// </summary>
    public class Zeroconf
    {
        private bool globalDone;
        private Socket listenSocket;
        private List<Socket> respondSockets;

        private List<object> listeners;
        private Dictionary<object, ServiceBrowser> browsers;
        private Dictionary<string, ServiceInfo> services;
        private Dictionary<string, int> serviceTypes;

        public DNSCache Cache { get; internal set; }

        private Engine engine;
        private Listener listener;
        private Reaper reaper;

        private object condition;
        // private bool debug;

        public bool Done => this.globalDone;

        /// <summary>
        /// Creates an instance of the Zeroconf class, establishing
        /// multicast communications, listening and reaping threads.
        /// </summary>
        /// <param name="interfaces">Interfaces.</param>
        public Zeroconf(InterfaceChoice interfaces=InterfaceChoice.All)
        {
            this.globalDone = false;

            /* LISTEN SOCKET START */
            this.listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram,
                                           ProtocolType.Udp);

            this.listenSocket.SetSocketOption(SocketOptionLevel.Socket,
                                  SocketOptionName.ReuseAddress,
                                  true);
            
            this.listenSocket.Bind(new IPEndPoint(IPAddress.Any, Constants.MDNS_PORT));

            MulticastOption mcastOption = new MulticastOption(
                Constants.MDNS_ADDRESS, IPAddress.Any
            );
            this.listenSocket.SetSocketOption(SocketOptionLevel.IP,
                                        SocketOptionName.AddMembership,
                                        mcastOption);
            /* LISTEN SOCKET END */

            /* RESPONSE SOCKET START */
            List<IPAddress> _interfaces = Utilities.NormalizeInterfaceChoice(
                                        interfaces,
                                        AddressFamily.InterNetwork);

            this.respondSockets = new List<Socket>();
            foreach(IPAddress localAddr in _interfaces)
            {
                Socket respondSocket = new Socket(AddressFamily.InterNetwork,
                                                  SocketType.Dgram, ProtocolType.Udp);

                respondSocket.SetSocketOption(SocketOptionLevel.Socket,
                      SocketOptionName.ReuseAddress,
                      true);

                respondSocket.Bind(new IPEndPoint(IPAddress.Any, Constants.MDNS_PORT));

                respondSocket.SetSocketOption(SocketOptionLevel.IP,
                                  SocketOptionName.AddMembership, new MulticastOption(Constants.MDNS_ADDRESS));

                respondSocket.SetSocketOption(SocketOptionLevel.IP,
                                  SocketOptionName.MulticastTimeToLive, 255);

                this.respondSockets.Add(respondSocket);
            }
            /* RESPONSE SOCKET END */

            this.listeners = new List<object>();
            this.browsers = new Dictionary<object, ServiceBrowser>();
            this.services = new Dictionary<string, ServiceInfo>();
            this.serviceTypes = new Dictionary<string, int>();

            this.Cache = new DNSCache();

            this.condition = new object();

            this.engine = new Engine(this);
            this.listener = new Listener(this);
            this.engine.AddReader(this.listener, this.listenSocket);
            this.reaper = new Reaper(this);

            // this.debug = false;
        }

        /// <summary>
        /// Calling thread waits for a given number of milliseconds or
        /// until notified.
        /// </summary>
        /// <returns>The wait.</returns>
        /// <param name="timeout">Timeout.</param>
        public void Wait(long timeout)
        {
            lock(this.condition)
            {
                Monitor.Wait(this.condition, (int)timeout);
            }
        }

        /// <summary>
        /// Notifies all waiting threads
        /// </summary>
        public void NotifyAll()
        {
            lock (this.condition)
            {
                Monitor.PulseAll(this.condition);
            }
        }

        /// <summary>
        /// Returns network's service information for a particular
        /// name and type, or null if no service matches by the timeout,
        /// which defaults to 3 seconds.
        /// </summary>
        /// <returns>The service info.</returns>
        /// <param name="type">Type.</param>
        /// <param name="name">Name.</param>
        /// <param name="timeout">Timeout.</param>
        public ServiceInfo GetServiceInfo(string type, string name, long timeout=3000)
        {
            ServiceInfo info = new ServiceInfo(type, name);
            if (info.Request(this, timeout))
                return info;
            return null;
        }

        /// <summary>
        /// Adds a listener for a particular service type.  This object
        /// will then have its update_record method called when information
        /// arrives for that type.
        /// </summary>
        /// <param name="type">Type.</param>
        /// <param name="listener">Listener.</param>
        public void AddServiceListener(string type, IListener listener)
        {
            this.RemoveServiceListener(listener);
            this.browsers[listener] = new ServiceBrowser(this, type,
                                                         listener: listener);
        }

        public void RemoveServiceListener(IListener listener)
        {
            if (!this.browsers.ContainsKey(listener))
                return;
            
            this.browsers[listener].Cancel();
            this.browsers.Remove(listener);
        }

        public void RemoveAllServiceListeners()
        {
            foreach (IListener l in this.browsers.Keys)
            {
                this.RemoveServiceListener(l);
            }
        }

        /// <summary>
        /// Registers service information to the network with a default TTL
        // of 60 seconds.Zeroconf will then respond to requests for
        // information for that service.The name of the service may be
        // changed if needed to make it unique on the network.
        /// </summary>
        /// <param name="info">Info.</param>
        /// <param name="ttl">Ttl.</param>
        /// <param name="allowNameChange">If set to <c>true</c> allow name change.</param>
        public void RegisterService(ServiceInfo info, ushort ttl=Constants.DNS_TTL,
                                   bool allowNameChange=false)
        {
            CheckService(info, allowNameChange);

            this.services[info.Name.ToLower()] = info;
            if (this.serviceTypes.ContainsKey(info.Type))
                this.serviceTypes[info.Type] += 1;
            else
                this.serviceTypes[info.Type] = 1;

            long now = Utilities.CurrentTimeMilliseconds();
            long nextTime = now;
            int i = 0;
            while (i < 3)
            {
                if (now < nextTime)
                {
                    Wait(nextTime - now);
                    now = Utilities.CurrentTimeMilliseconds();
                    continue;
                }
                DNSOutgoing outgoing = new DNSOutgoing((ushort)QueryFlags.Response | (ushort)Flags.AA);
                outgoing.AddAnswerAtTime(
                    new DNSPointer(info.Type, DNSType.PTR, DNSClass.IN, ttl, info.Name), 0);
                outgoing.AddAnswerAtTime(
                    new DNSService(info.Name, DNSType.SRV, DNSClass.IN, ttl,
                                   info.Priority, info.Weight, info.Port, info.Server), 0);

                outgoing.AddAnswerAtTime(
                    new DNSText(info.Name, DNSType.TXT, DNSClass.IN, ttl, info.Text), 0);
                if (info.Address != null)
                {
                    outgoing.AddAnswerAtTime(
                        new DNSAddress(info.Server, DNSType.A, DNSClass.IN, ttl,
                                       Utilities.AddressToBytes(info.Address)), 0);
                }

                Send(outgoing);
                i++;
                nextTime += Timing.Register;
            }
        }

        /// <summary>
        /// Unregister a service.
        /// </summary>
        /// <param name="info">Info.</param>
        public void UnregisterService(ServiceInfo info)
        {
            string nameLower = info.Type.ToLower();
            if (this.services.ContainsKey(nameLower))
            {
                this.services.Remove(nameLower);
                if (this.serviceTypes[info.Type] > 0)
                    this.serviceTypes[info.Type] -= 1;
                else
                    this.serviceTypes.Remove(info.Type);
            }

            long now = Utilities.CurrentTimeMilliseconds();
            long nextTime = now;
            int i = 0;

            while (i < 3)
            {
                if (now < nextTime)
                {
                    Wait(nextTime - now);
                    now = Utilities.CurrentTimeMilliseconds();
                    continue;
                }
                DNSOutgoing outgoing = new DNSOutgoing((ushort)QueryFlags.Response | (ushort)Flags.AA);
                outgoing.AddAnswerAtTime(
                    new DNSPointer(info.Type, DNSType.PTR, DNSClass.IN, 0, info.Name), 0);
                outgoing.AddAnswerAtTime(
                    new DNSService(info.Name, DNSType.SRV, DNSClass.IN, 0,
                                   info.Priority, info.Weight, info.Port, info.Name), 0);

                outgoing.AddAnswerAtTime(
                    new DNSText(info.Name, DNSType.TXT, DNSClass.IN, 0, info.Text), 0);
                if (info.Address != null)
                {
                    outgoing.AddAnswerAtTime(
                        new DNSAddress(info.Server, DNSType.A, DNSClass.IN, 0,
                                       Utilities.AddressToBytes(info.Address)), 0);
                }

                Send(outgoing);
                i++;
                nextTime += Timing.Unregister;
            }
        }

        /// <summary>
        /// Unregister all registered services.
        /// </summary>
        public void UnregisterAllServices()
        {
            if (this.services.Count == 0)
                return;

            foreach(ServiceInfo info in this.services.Values)
            {
                UnregisterService(info);
            }
        }

        /// <summary>
        /// Checks the network for a unique service name, modifying the
        /// ServiceInfo passed in if it is not unique.
        /// </summary>
        /// <param name="info">Info.</param>
        /// <param name="allowNameChange">If set to <c>true</c> allow name change.</param>
        public void CheckService(ServiceInfo info, bool allowNameChange=false)
        {
            // This is kind of funky because of the subtype based tests
            // need to make subtypes a first class citizen
            string serviceName = Utilities.ServiceTypeName(info.Name);
            if (!info.Type.EndsWith(serviceName, StringComparison.CurrentCulture))
                throw new BadTypeInNameException("CheckService");

            string instanceName = info.Name.Substring(0, serviceName.Length - 1);
            int nextInstanceNumber = 2;

            long now = Utilities.CurrentTimeMilliseconds();
            long nextTime = now;
            int i = 0;

            while (i < 3)
            {
                // Check for a name conflict
                while (this.Cache.CurrentEntryWithNameAndAlias(info.Type, info.Name) != null)
                {
                    if (!allowNameChange)
                        throw new NonUniqueNameException("CheckService");

                    info.Name = String.Format("{0}-{1}.{2}",
                                              instanceName,
                                              nextInstanceNumber,
                                              info.Type);
                    nextInstanceNumber += 1;
                    Utilities.ServiceTypeName(info.Name);
                    nextTime = now;
                    i = 0;
                }

                if (now < nextTime)
                {
                    Wait(nextTime - now);
                    now = Utilities.CurrentTimeMilliseconds();
                    continue;
                }

                DNSOutgoing outgoing = new DNSOutgoing((ushort)QueryFlags.Query | (ushort)Flags.AA);
                //this.debug = outgoing;
                outgoing.AddQuestion(new DNSQuestion(info.Type, DNSType.PTR, DNSClass.IN));
                outgoing.AddAuthorativeAnswer(new DNSPointer(
                    info.Type, DNSType.PTR, DNSClass.IN, Constants.DNS_TTL, info.Name
                ));
                Send(outgoing);
                i++;
                nextTime += Timing.Check;
            }
        }

        /// <summary>
        /// Adds a listener for a given question.  The listener will have
        /// its update_record method called when information is available to
        /// answer the question.
        /// </summary>
        /// <param name="listener">Listener.</param>
        /// <param name="question">Question.</param>
        public void AddListener(IServiceListener listener, DNSQuestion question)
        {
            long now = Utilities.CurrentTimeMilliseconds();
            this.listeners.Add(listener);
            if (question != null)
            {
                foreach(DNSRecord record in this.Cache.EntriesWithName(question.Name))
                {
                    if (question.AnsweredBy(record) && !record.IsExpired(now))
                        listener.UpdateRecord(this, now, record);
                }
            }
            NotifyAll();
        }

        /// <summary>
        /// Removes a listener
        /// </summary>
        /// <param name="listener">Listener.</param>
        public void RemoveListener(IServiceListener listener)
        {
            if (!this.listeners.Contains(listener))
                return;
            
            this.listeners.Remove(listener);
            NotifyAll();
        }

        /// <summary>
        /// Used to notify listeners of new information that has updated
        // a record.
        /// </summary>
        /// <param name="now">Now.</param>
        /// <param name="record">Record.</param>
        public void UpdateRecord(long now, DNSRecord record)
        {
            foreach(IServiceListener l in this.listeners)
            {
                l.UpdateRecord(this, now, record);
            }
            NotifyAll();
        }

        /// <summary>
        /// Deal with incoming response packets.  All answers
        /// are held in the cache, and listeners are notified.
        /// </summary>
        /// <param name="msg">Message.</param>
        public void HandleResponse(DNSIncoming msg)
        {
            long now = Utilities.CurrentTimeMilliseconds();
            foreach (DNSRecord record in msg.Answers)
            {
                bool expired = record.IsExpired(now);
                if (this.Cache.Entries().Contains(record))
                {
                    if (expired)
                        this.Cache.Remove(record);
                    else
                    {
                        DNSRecord entry = (DNSRecord)this.Cache.Get(record);
                        if (entry != null)
                            entry.ResetTTL(record);
                    }
                }
                else
                    this.Cache.Add(record);
            }

            foreach (DNSRecord record in msg.Answers)
            {
                UpdateRecord(now, record);
            }
        }

        /// <summary>
        /// Deal with incoming query packets.  Provides a response if
        /// possible.
        /// </summary>
        /// <param name="msg">Message.</param>
        /// <param name="address">Address.</param>
        /// <param name="port">Port.</param>
        public void HandleQuery(DNSIncoming msg, IPAddress address, int port)
        {
            DNSOutgoing outgoing = null;

            // Support unicast client responses
            if (port != Constants.MDNS_PORT)
            {
                outgoing = new DNSOutgoing((ushort)QueryFlags.Response | (ushort)Flags.AA,
                                           multicast: false);
                foreach (DNSQuestion question in msg.Questions)
                    outgoing.AddQuestion(question);
            }

            foreach (DNSQuestion question in msg.Questions)
            {
                if (question.Type == DNSType.PTR)
                {
                    if (question.Name == "_services._dns-sd._udp.local.")
                    {
                        foreach (string stype in this.serviceTypes.Keys)
                        {
                            if (outgoing == null)
                                outgoing = new DNSOutgoing((ushort)QueryFlags.Response | (ushort)Flags.AA);
                            outgoing.AddAnswer(msg, new DNSPointer(
                                "_services._dns-sd._udp.local.", DNSType.PTR,
                                DNSClass.IN, Constants.DNS_TTL, stype));
                        }
                    }

                    foreach (ServiceInfo service in this.services.Values)
                    {
                        if (question.Name == service.Type)
                        {
                            if (outgoing == null)
                                outgoing = new DNSOutgoing((ushort)QueryFlags.Response | (ushort)Flags.AA);
                            outgoing.AddAnswer(msg, new DNSPointer(
                                service.Type, DNSType.PTR,
                                DNSClass.IN, Constants.DNS_TTL, service.Name));
                        }
                    }
                }
                else
                {
                    if (outgoing == null)
                        outgoing = new DNSOutgoing((ushort)QueryFlags.Response | (ushort)Flags.AA);

                    // Answer A record queries for any service addresses we know
                    if (question.Type == DNSType.A || question.Type == DNSType.ANY)
                    {
                        foreach (ServiceInfo s in this.services.Values)
                        {
                            if (s.Server == question.Name.ToLower())
                                outgoing.AddAnswer(msg, new DNSAddress(
                                    question.Name, DNSType.A,
                                    DNSClass.IN | DNSClass.UNIQUE,
                                    Constants.DNS_TTL,
                                    Utilities.AddressToBytes(s.Address)));
                        }
                    }

                    if (!this.services.ContainsKey(question.Name.ToLower()))
                        continue;

                    ServiceInfo service = this.services[question.Name.ToLower()];

                    if (question.Type == DNSType.SRV || question.Type == DNSType.ANY)
                    {
                        outgoing.AddAnswer(msg, new DNSService(
                            question.Name, DNSType.SRV, DNSClass.IN | DNSClass.UNIQUE,
                            Constants.DNS_TTL, service.Priority, service.Weight,
                            service.Port, service.Server));
                        
                    }
                    if (question.Type == DNSType.TXT || question.Type == DNSType.ANY)
                    {
                        outgoing.AddAnswer(msg, new DNSText(
                            question.Name, DNSType.TXT, DNSClass.IN | DNSClass.UNIQUE,
                            Constants.DNS_TTL, service.Text));

                    }
                    if (question.Type == DNSType.SRV)
                    {
                        outgoing.AddAdditionalAnswer(new DNSAddress(
                            service.Server, DNSType.A, DNSClass.IN | DNSClass.UNIQUE,
                            Constants.DNS_TTL, Utilities.AddressToBytes(service.Address)));
                    }
                }
            }
            if (outgoing != null && outgoing.Answers.Count > 0)
            {
                outgoing.ID = msg.ID;
                Send(outgoing, address, port);
            }
        }

        /// <summary>
        /// Send an outgoing packet
        /// </summary>
        /// <returns>The send.</returns>
        /// <param name="outgoing">Outgoing.</param>
        /// <param name="address">Address.</param>
        /// <param name="port">Port.</param>
        public void Send(DNSOutgoing outgoing, IPAddress address=null, int port=Constants.MDNS_PORT)
        {
            if (address == null)
                address = Constants.MDNS_ADDRESS;

            byte[] packet = outgoing.Packet();
            if (packet.Length > Constants.MAX_MSG_ABSOLUTE)
            {
                Console.WriteLine("Dropping {0} over-sized packet ({1} bytes packet",
                                  outgoing, packet.Length);
                return; 
            }
            foreach(Socket s in this.respondSockets)
            {
                if (this.globalDone)
                    return;
                int sent = s.SendTo(packet, new IPEndPoint(address, port));
                if (sent != packet.Length)
                {
                    Console.WriteLine("!!! sent {0} out of {1} bytes to {2}",
                                      sent, packet.Length, s);
                }
            }
                                    
        }

        public void Close()
        {
            if (this.globalDone)
                return;

            this.globalDone = true;

            // Remove service listeners
            RemoveAllServiceListeners();
            UnregisterAllServices();

            // Shutdown Receive-socket and thread
            this.engine.DeleteReader(this.listenSocket);

            this.listenSocket.Close();
            this.engine.Join();

            // Shutdown the rest
            NotifyAll();
            this.reaper.Join();
            foreach (Socket s in this.respondSockets)
                s.Close();
        }
    }
}
