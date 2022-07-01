
namespace EEBUS.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.Net.Security;
    using System.Net.WebSockets;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public class BrowserController : Controller
    {
        private readonly MDNSClient _mDNSClient;
        private ClientWebSocket _wsClient;

        public BrowserController(MDNSClient mDNSClient)
        {
            _mDNSClient = mDNSClient;
        }

        public IActionResult Index()
        {
            return View("Index", _mDNSClient.getEEBUSNodes());
        }

        private bool ValidateServerCert(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            // TODO: always accept for now
            return true;
        }

        [HttpPost]
        public async Task<IActionResult> Connect()
        {
            try
            {
                foreach (string key in Request.Form.Keys)
                {
                    if (key.Contains("Endpoint:"))
                    {
                        string[] parts = key.Split(' ');
                        break;
                    }
                }

                _wsClient = new ClientWebSocket();
                _wsClient.Options.AddSubProtocol("ship");
                _wsClient.Options.RemoteCertificateValidationCallback = ValidateServerCert;
                X509Certificate2 cert = CertificateGenerator.GenerateCert();
                _wsClient.Options.ClientCertificates.Add(cert);
                await _wsClient.ConnectAsync(new Uri("wss://localhost:50000/eebus"), CancellationToken.None).ConfigureAwait(false);
       
                var buffer = new byte[256];
                if (_wsClient.State == WebSocketState.Open)
                {
                    await _wsClient.SendAsync(Encoding.UTF8.GetBytes("Hello"), WebSocketMessageType.Binary, true, CancellationToken.None).ConfigureAwait(false);
                    WebSocketReceiveResult result = await _wsClient.ReceiveAsync(buffer, CancellationToken.None).ConfigureAwait(false);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await _wsClient.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None).ConfigureAwait(false);
                    }
                    else
                    {
                        await _wsClient.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None).ConfigureAwait(false);
                        return View("Index", new string[] { "Received '" + Encoding.UTF8.GetString(buffer, 0, result.Count) + "' from server!" });
                    }
                }

                _wsClient.Dispose();

                return View("Index", _mDNSClient.getEEBUSNodes());
            }
            catch (Exception ex)
            {
                return View("Index", new string[] { "Error: " + ex.Message });
            }
        }
    }
}
