using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace Zeroconf
{
    /// <summary>
    /// Service information
    /// </summary>
    public class ServiceInfo : IServiceListener
    {
        public Dictionary<string, string> Properties { get; internal set; }
        public string Type { get; internal set; }
        public string Name { get; internal set; }
        public string Address { get; internal set; }
        public ushort Port { get; internal set; }
        public ushort Weight { get; internal set; }
        public ushort Priority { get; internal set; }
        public string Server { get; internal set; }

        public byte[] Text { get; internal set; }

        /// <summary>
        /// Create a service description.
        /// Initializes a new instance of the <see cref="T:Zeroconf.ServiceInfo"/> class.
        /// </summary>
        /// <param name="type">Fully qualified service type name</param>
        /// <param name="name">Fully qualified service name</param>
        /// <param name="address">IP address</param>
        /// <param name="port">Port that the service runs on</param>
        /// <param name="weight">Weight of the service</param>
        /// <param name="priority">Priority of the service</param>
        /// <param name="properties">Dictionary of properties</param>
        /// <param name="server">Fully qualified name for service host(defaults to name)</param>
        public ServiceInfo(string type, string name,
                           string address = null, ushort port = 0,
                           ushort weight = 0, ushort priority = 0,
                           Dictionary<string, string> properties = null,
                           string server = null)
        {
            if (!type.EndsWith(Utilities.ServiceTypeName(name), StringComparison.CurrentCulture))
                throw new BadTypeInNameException("ServiceInfo init");

            this.Text = null;

            this.Type = type;
            this.Name = name;
            this.Address = address;
            this.Port = port;
            this.Weight = weight;
            this.Priority = priority;
            if (server != null)
                this.Server = server;
            else
                this.Server = name;
            
            this.Properties = new Dictionary<string, string>();
            SetProperties(properties);
        }

        /// <summary>
        /// Sets properties and text of this info from a dictionary
        /// </summary>
        /// <param name="properties">Properties.</param>
        private void SetProperties(Dictionary<string, string> properties)
        {
            if (properties == null || properties.Count == 0)
                return;

            this.Properties = properties;

            MemoryStream ms = new MemoryStream();
            BigEndianWriter bw = new BigEndianWriter(ms);

            foreach(KeyValuePair<string,string> kv in this.Properties)
            {
                string pair = kv.Key + "=" + kv.Value;
                bw.WritePrefixed(pair);
            }

            this.Text = ms.ToArray();
        }

        /// <summary>
        /// Sets properties and text given a text field
        /// </summary>
        /// <param name="text">Text.</param>
        private void SetText(byte[] text)
        {
            if (text == null || text.Length == 0)
                return;

            this.Text = text;

            BigEndianReader br = new BigEndianReader(text);

            while (br.BaseStream.Position < br.BaseStream.Length)
            {
                string[] kv = br.ReadPrefixedString().Split('=');
                if (kv.Length != 2)
                    throw new Exception("Invalid keyValuePair");

                // Skip existing entries
                if (this.Properties.ContainsKey(kv[0]))
                    continue;
                
                this.Properties.Add(kv[0], kv[1]);
            }
        }

        /// <summary>
        /// Name accessor
        /// </summary>
        /// <returns>The name.</returns>
        public string GetName()
        {
            if (this.Type != null && this.Name.EndsWith("." + this.Type, StringComparison.CurrentCulture))
            {
                int length = this.Name.Length - this.Type.Length - 1;
                return this.Name.Substring(0, length);
            }
            return this.Name;
        }

        /// <summary>
        /// Updates service information from a DNS record
        /// </summary>
        /// <param name="zc">Zc.</param>
        /// <param name="now">Now.</param>
        /// <param name="record">Record.</param>
        public void UpdateRecord(Zeroconf zc, long now, DNSRecord record)
        {
            if (record != null && !record.IsExpired(now))
            {
                if (record.Type == DNSType.A && record.Name == this.Server)
                {
                    this.Address = ((DNSAddress)record).Address.ToString();
                }
                else if (record.Type == DNSType.SRV && record.Name == this.Name)
                {
                    this.Server = ((DNSService)record).Server.ToString();
                    this.Port = ((DNSService)record).Port;
                    this.Weight = ((DNSService)record).Weight;
                    this.Priority = ((DNSService)record).Priority;
                    UpdateRecord(zc, now, (DNSAddress)zc.Cache.GetByDetails(
                        this.Server,
                        DNSType.A,
                        DNSClass.IN));
                }
                else if (record.Type == DNSType.TXT && record.Name == this.Name)
                    SetText(((DNSText)record).Text);
            }
        }

        public bool Request(Zeroconf zc, long timeout)
        {
            long now = Utilities.CurrentTimeMilliseconds();
            long delay = Timing.Listener;
            long next = now + delay;
            long last = now + timeout;

            List<Tuple<DNSType, DNSClass>> recordTypesForCheckCache = new List<Tuple<DNSType, DNSClass>>()
            {
                new Tuple<DNSType, DNSClass>(DNSType.SRV, DNSClass.IN),
                new Tuple<DNSType, DNSClass>(DNSType.TXT, DNSClass.IN),
            };

            if (this.Server != null)
                recordTypesForCheckCache.Add(new Tuple<DNSType, DNSClass>(DNSType.A, DNSClass.IN));

            foreach(Tuple<DNSType,DNSClass> record_type in recordTypesForCheckCache)
            {
                DNSRecord cached = (DNSRecord)zc.Cache.GetByDetails(this.Name,
                                                         record_type.Item1,
                                                         record_type.Item2);
                if (cached != null)
                    this.UpdateRecord(zc, now, cached);
            }

            if (this.Server != null || this.Address != null || this.Text != null)
                return true;

            try
            {
                zc.AddListener(this, new DNSQuestion(this.Name, DNSType.ANY, DNSClass.IN));
                while (this.Server == null || this.Address == null ||this.Text == null)
                {
                    if (last <= now)
                        return false;
                    if (next <= now)
                    {
                        DNSOutgoing outgoing = new DNSOutgoing((ushort)QueryFlags.Query);

                        outgoing.AddQuestion(
                            new DNSQuestion(this.Name, DNSType.SRV, DNSClass.IN));
                        outgoing.AddAnswerAtTime(
                            (DNSService)zc.Cache.GetByDetails(this.Name, DNSType.SRV, DNSClass.IN),
                            now
                        );

                        outgoing.AddQuestion(
                            new DNSQuestion(this.Name, DNSType.TXT, DNSClass.IN));
                        outgoing.AddAnswerAtTime(
                            (DNSText)zc.Cache.GetByDetails(this.Name, DNSType.TXT, DNSClass.IN),
                            now
                        );

                        if (this.Server != null)
                        {
                            outgoing.AddQuestion(
                                new DNSQuestion(this.Name, DNSType.A, DNSClass.IN));
                            outgoing.AddAnswerAtTime(
                                (DNSAddress)zc.Cache.GetByDetails(this.Name, DNSType.A, DNSClass.IN),
                                now
                            );
                        }

                        zc.Send(outgoing);
                        next = now + delay;
                        delay *= 2;
                    }
                    zc.Wait(Math.Min(next, last) - now);
                    now = Utilities.CurrentTimeMilliseconds();
                }
            }
            finally
            {
                zc.RemoveListener(this);
            }
            return true;
        }

        public override bool Equals(object obj)
        {
            ServiceInfo other = obj as ServiceInfo;
            return (this.Name == other.Name);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("[ServiceInfo] ");
            sb.AppendFormat("type={0}, ", this.Type);
            sb.AppendFormat("name={0}, ", this.Name);
            sb.AppendFormat("address={0}, ", this.Address);
            sb.AppendFormat("port={0}, ", this.Port);
            sb.AppendFormat("weight={0}, ", this.Weight);
            sb.AppendFormat("priority={0}, ", this.Priority);
            sb.AppendFormat("server={0}, ", this.Server);
            sb.AppendFormat("props={0}, ", this.Properties);
            return sb.ToString();
        }
    }
}
