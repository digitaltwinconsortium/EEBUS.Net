
using Makaretu.Dns;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EEBUS
{
    public class MDNSClient
    {
        private ConcurrentDictionary<string, DateTime> _currentEEBUSNodes = new ConcurrentDictionary<string, DateTime>();

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

                        // purge all records that are more than 1 hour old
                        foreach(KeyValuePair<string, DateTime> t in _currentEEBUSNodes)
                        {
                            if (t.Value < DateTime.UtcNow.Add(new TimeSpan(-1,0,0)))
                            {
                                _currentEEBUSNodes.TryRemove(t);
                            }
                        }

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

        public string[] getEEBUSNodes()
        {
            return _currentEEBUSNodes.Keys.ToArray();
        }

        private void Sd_ServiceInstanceDiscovered(object sender, ServiceInstanceDiscoveryEventArgs e)
        {
            if (e.ServiceInstanceName.ToString().Contains("._ship."))
            {
                Console.WriteLine($"EEBUS service instance '{e.ServiceInstanceName}' discovered.");

                if (_currentEEBUSNodes.ContainsKey(e.ServiceInstanceName.ToString()))
                {
                    _currentEEBUSNodes[e.ServiceInstanceName.ToString()] = DateTime.UtcNow;
                }
                else
                {
                    _currentEEBUSNodes.TryAdd(e.ServiceInstanceName.ToString(), DateTime.UtcNow);
                }
            }
        }
    }
}
