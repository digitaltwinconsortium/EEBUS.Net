
namespace EEBUS.Controllers
{
    using EEBUS.Models;
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.Net;
    using System.Net.Security;
    using System.Net.WebSockets;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public class BrowserController : Controller
    {
        private readonly MDNSClient _mDNSClient;
        private static ClientWebSocket _wsClient;
        private ServerNode _model = new ServerNode();

        public BrowserController(MDNSClient mDNSClient)
        {
            _mDNSClient = mDNSClient;
        }

        public IActionResult Index()
        {
            try
            {
                return View("Index", _mDNSClient.getEEBUSNodes());
            }
            catch (Exception ex)
            {
                _model.Error = "Error: " + ex.Message;
                return View("Index", _model);
            }
        }

        private bool ValidateServerCert(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            // extract SKI
            foreach (X509Extension extension in ((X509Certificate2)certificate).Extensions)
            {
                if (extension.Oid.FriendlyName == "Subject Key Identifier")
                {
                    X509SubjectKeyIdentifierExtension ext = (X509SubjectKeyIdentifierExtension)extension;
                    _model.SKI = ext.SubjectKeyIdentifier;
                    break;
                }
            }

            return true;
        }

        [HttpPost]
        public async Task<IActionResult> Connect()
        {
            try
            {
                foreach (string key in Request.Form.Keys)
                {
                    if (key.Contains("EEBUS:"))
                    {
                        string[] parts = key.Split(' ');
                        _model.Url = parts[1];
                        _model.Name = parts[2];
                        break;
                    }
                }

                _wsClient = new ClientWebSocket();
                _wsClient.Options.AddSubProtocol("ship");
                _wsClient.Options.RemoteCertificateValidationCallback = ValidateServerCert;
                X509Certificate2 cert = CertificateGenerator.GenerateCert(Dns.GetHostName());
                _wsClient.Options.ClientCertificates.Add(cert);
                await _wsClient.ConnectAsync(new Uri("wss://" + _model.Url), CancellationToken.None).ConfigureAwait(false);
                
                return View("Accept", _model);
            }
            catch (Exception ex)
            {
                _model.Error = "Error: " + ex.Message;
                return View("Index", new ServerNode[] { _model });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Accept()
        {
            try
            {
                if (_wsClient.State == WebSocketState.Open)
                {
                    await _wsClient.SendAsync(Encoding.UTF8.GetBytes("Hello"), WebSocketMessageType.Binary, true, CancellationToken.None).ConfigureAwait(false);

                    var buffer = new byte[256]; 
                    WebSocketReceiveResult result = await _wsClient.ReceiveAsync(buffer, CancellationToken.None).ConfigureAwait(false);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        return await Disconnect().ConfigureAwait(false);
                    }

                    _model.LastMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    return View("Connected", _model);
                }
                else
                {
                    return await Disconnect().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _model.Error = "Error: " + ex.Message;
                return View("Index", new ServerNode[] { _model });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Disconnect()
        {
            try
            {
                if (_wsClient != null)
                {
                    await _wsClient.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None).ConfigureAwait(false);
                    _wsClient.Dispose();
                    _wsClient = null;
                }
                
                return View("Index", _mDNSClient.getEEBUSNodes());
            }
            catch (Exception)
            {
                return View("Index", _mDNSClient.getEEBUSNodes());
            }
        }
    }
}
