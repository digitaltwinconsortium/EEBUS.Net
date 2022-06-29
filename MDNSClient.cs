
using Makaretu.Dns;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EEBUS
{
    public class MDNSClient
    {
        public void Run()
        {
            _ = Task.Run(async() =>
            {
                Thread.CurrentThread.IsBackground = true;

                MulticastService mdns = new MulticastService();
                ServiceDiscovery sd = new ServiceDiscovery(mdns);

                sd.ServiceDiscovered += (s, serviceName) => { mdns.SendQuery(serviceName); };
                sd.ServiceInstanceDiscovered += Sd_ServiceInstanceDiscovered;

                try
                {
                    mdns.Start();

                    while (true)
                    {
                        sd.QueryAllServices();

                        await Task.Delay(5000).ConfigureAwait(false);
                    }
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

        private void Sd_ServiceInstanceDiscovered(object sender, ServiceInstanceDiscoveryEventArgs e)
        {
            Console.WriteLine($"service instance '{e.ServiceInstanceName}' discovered:");
            Console.WriteLine(e.Message.ToString());
            Console.WriteLine("-------------------------------------------------------");
        }
    }
}
