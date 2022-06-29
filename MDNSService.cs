
using Makaretu.Dns;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EEBUS
{
    public class MDNSService
    {
        private ServiceProfile serviceProfile = new ServiceProfile("Microsoft Azure EEBUS Gateway", "_ship._tcp", 50000);

        public void AddProperty(string key, string value)
        {
            serviceProfile.AddProperty(key, value);
        }

        public void Run()
        {
            _ = Task.Run(async() =>
            {
                Thread.CurrentThread.IsBackground = true;

                MulticastService mdns = new MulticastService();
                ServiceDiscovery sd = new ServiceDiscovery(mdns);
                
                try
                {
                    mdns.Start();

                    sd.Advertise(serviceProfile);

                    await Task.Delay(-1).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                finally
                {
                    sd.Dispose();
                    mdns.Stop();
                }
            });
        }
    }
}
