using System;
using System.Threading;

namespace Zeroconf
{
    public class Reaper
    {
        private Thread t;
        private Zeroconf zc;

        public Reaper(Zeroconf zc)
        {
            this.t = new Thread(Run)
            {
                Name = "zeroconf-Reaper",
                IsBackground = true
            };
            this.zc = zc;
            this.t.Start();
        }

        public void Run()
        {
            while (true)
            {
                this.zc.Wait(10 * 1000);
                if (this.zc.Done)
                    return;
                
                long now = Utilities.CurrentTimeMilliseconds();
                foreach (DNSRecord record in this.zc.Cache.Entries())
                {
                    if (record.IsExpired(now))
                    {
                        this.zc.UpdateRecord(now, record);
                        this.zc.Cache.Remove(record);
                    }
                }
            }
        }

        public void Join()
        {
            this.t.Join();
        }
    }
}
