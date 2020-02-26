using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;

namespace Zeroconf
{
    /// <summary>
    /// Used to browse for a service of a specific type.
    ///
    /// The listener object will have its add_service() and
    /// remove_service() methods called when this browser
    /// discovers changes in the services availability.
    /// </summary>
    public class ServiceBrowser : IServiceListener
    {
        private Thread t;
        private Zeroconf zc;
        private string type;
        private Dictionary<string, DNSRecord> services;
        private long nextTime;
        private long delay;

        private event Delegates.HandlerDelegate _handlersToCall;
        private Queue<Delegates.OnChangeEventArgs> eventArgs;

        private bool done;

        public ServiceBrowser(Zeroconf zc, string type,
                              List<Delegates.HandlerDelegate> handlers=null,
                              IListener listener=null)
        {
            if (handlers == null && listener == null)
            {
                throw new Exception("You need to specify at least one handler");
            }

            if (!type.EndsWith(Utilities.ServiceTypeName(type), StringComparison.CurrentCulture))
                throw new BadTypeInNameException("");

            this.t = new Thread(Run)
            {
                Name = "zeroconf-ServiceBrowser_" + type,
                IsBackground = true
            };

            this.zc = zc;
            this.type = type;
            this.services = new Dictionary<string, DNSRecord>();
            this.nextTime = Utilities.CurrentTimeMilliseconds();
            this.delay = Timing.Browser;

            this.done = false;

            if (handlers != null)
            {
                foreach (Delegates.HandlerDelegate h in handlers)
                {
                    this._handlersToCall += h;   
                }
            }

            if(listener != null)
            {
                Delegates.HandlerDelegate OnChange = (sender, e) => {
                    Zeroconf _zc = sender as Zeroconf;
                    if (e.StateChange == ServiceStateChange.Added)
                        listener.AddService(_zc, e.Type, e.Name);
                    else if (e.StateChange == ServiceStateChange.Removed)
                        listener.RemoveService(_zc, e.Type, e.Name);
                    else
                        throw new NotImplementedException(e.StateChange.ToString());
                };

                this._handlersToCall += OnChange;
            }
            this.eventArgs = new Queue<Delegates.OnChangeEventArgs>();

            this.t.Start();
        }

        /// <summary>
        /// Callback invoked by Zeroconf when new information arrives.
        /// Updates information required by browser in the Zeroconf cache.
        /// </summary>
        /// <param name="zc">Zc.</param>
        /// <param name="now">Now.</param>
        /// <param name="record">Record.</param>
        public void UpdateRecord(Zeroconf zc, long now, DNSRecord record)
        {
            if (record.Type == DNSType.PTR && record.Name == this.type)
            {
                bool expired = record.IsExpired(now);
                string alias = ((DNSPointer)record).Alias;
                string serviceKey = alias.ToLower();
                DNSRecord oldRecord;

                bool success = this.services.TryGetValue(serviceKey, out oldRecord);
                if (!success && !expired)
                {
                    this.services[serviceKey] = record;
                    this.eventArgs.Enqueue(
                        new Delegates.OnChangeEventArgs(this.type, alias,
                                                        ServiceStateChange.Added)
                    );
                }
                else if (!expired)
                {
                    oldRecord.ResetTTL(record);
                }
                else
                {
                    this.services.Remove(serviceKey);
                    this.eventArgs.Enqueue(
                        new Delegates.OnChangeEventArgs(this.type, alias,
                                                        ServiceStateChange.Removed)
                    );
                    return;
                }

                long expires = record.GetExpirationTime(75);
                if (expires < this.nextTime)
                    this.nextTime = expires;
            }
        }

        public void Cancel()
        {
            this.done = true;
            this.zc.RemoveListener(this);
            this.t.Join();
        }

        public void Run()
        {
            this.zc.AddListener(this, new DNSQuestion(this.type,
                                                      DNSType.PTR, DNSClass.IN));

            long now;
            while (true)
            {
                now = Utilities.CurrentTimeMilliseconds();
                if (this.eventArgs.Count == 0 && this.nextTime > now)
                    this.zc.Wait(this.nextTime - now);
                if (this.zc.Done || this.done)
                    return;
                now = Utilities.CurrentTimeMilliseconds();
                if (this.nextTime <= now)
                {
                    DNSOutgoing outgoing = new DNSOutgoing((ushort)QueryFlags.Query);
                    outgoing.AddQuestion(new DNSQuestion(this.type, DNSType.PTR, DNSClass.IN));
                    foreach (DNSRecord record in this.services.Values)
                    {
                        if (!record.IsStale(now))
                        {
                            outgoing.AddAnswerAtTime(record, now);
                        }
                    }
                    this.zc.Send(outgoing);
                    this.nextTime = now + this.delay;
                    this.delay = Math.Min(20 * 1000, this.delay * 2);
                }

                if (this.eventArgs.Count > 0 && !this.zc.Done)
                {
                    Delegates.OnChangeEventArgs args = this.eventArgs.Dequeue();
                    this._handlersToCall(this.zc, args);
                }
            }
        }

        public void Join()
        {
            this.t.Join();
        }
    }
}
