
using EEBUS.Models;
using Makaretu.Dns;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace EEBUS
{
    public class MDNSClient
    {
        private ConcurrentDictionary<ServerNode, DateTime> _currentEEBUSNodes = new ConcurrentDictionary<ServerNode, DateTime>();

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
                        foreach(KeyValuePair<ServerNode, DateTime> t in _currentEEBUSNodes)
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

        public ServerNode[] getEEBUSNodes()
        {
            return _currentEEBUSNodes.Keys.ToArray();
        }

        private void Sd_ServiceInstanceDiscovered(object sender, ServiceInstanceDiscoveryEventArgs e)
        {
            if (e.ServiceInstanceName.ToString().Contains("._ship."))
            {
                Console.WriteLine($"EEBUS service instance '{e.ServiceInstanceName}' discovered.");

                IEnumerable<SRVRecord> servers = e.Message.AdditionalRecords.OfType<SRVRecord>();
                IEnumerable<AddressRecord> addresses = e.Message.AdditionalRecords.OfType<AddressRecord>();
                IEnumerable<string> txtRecords = e.Message.AdditionalRecords.OfType<TXTRecord>()?.SelectMany(s => s.Strings);
                
                if (servers?.Count() > 0 && addresses?.Count() > 0 && txtRecords?.Count() > 0)
                {
                    foreach (SRVRecord server in servers)
                    {
                        IEnumerable<AddressRecord> serverAddresses = addresses.Where(w => w.Name == server.Target);
                        if (serverAddresses?.Count() > 0)
                        {
                            foreach (AddressRecord serverAddress in serverAddresses)
                            {
                                // we only want IPv4 addresses
                                if (serverAddress.Address.AddressFamily == AddressFamily.InterNetwork)
                                {
                                    string id = string.Empty;
                                    string path = string.Empty;
                                    foreach (string textRecord in txtRecords)
                                    {
                                        if (textRecord.StartsWith("path"))
                                        {
                                            path = textRecord.Substring(textRecord.IndexOf('=') + 1);
                                        }

                                        if (textRecord.StartsWith("id"))
                                        {
                                            id = textRecord.Substring(textRecord.IndexOf('=') + 1);
                                        }
                                    }

                                    if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(path))
                                    {
                                        ServerNode newNode = new ServerNode
                                        {
                                            Name = e.ServiceInstanceName.ToString(),
                                            Url = serverAddress.Address.ToString() + ":" + server.Port.ToString() + path,
                                            Id = id
                                        };

                                        if (_currentEEBUSNodes.ContainsKey(newNode))
                                        {
                                            _currentEEBUSNodes[newNode] = DateTime.UtcNow;
                                        }
                                        else
                                        {
                                            _currentEEBUSNodes.TryAdd(newNode, DateTime.UtcNow);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                
            }
        }
    }
}
