
namespace EEBUS.Controllers
{
    using EEBUS.Enums;
    using EEBUS.Models;
    using Microsoft.AspNetCore.Mvc;
    using Newtonsoft.Json;
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

                    // add spaces every 4 hex digits (EEBUS requirement)
                    for (int i = 4; i < _model.SKI.Length; i += 4)
                    {
                        _model.SKI = _model.SKI.Insert(i, " ");
                        i++;
                    }

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
                        _model.Name = parts[1];
                        _model.Id = parts[2];
                        _model.Url = parts[3];
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
                foreach (string key in Request.Form.Keys)
                {
                    if (key.Contains("EEBUS:"))
                    {
                        string[] parts = key.Split(' ');
                        _model.Name = parts[1];
                        _model.Id = parts[2];
                        _model.Url = parts[3];
                        break;
                    }
                }

                if (_wsClient.State == WebSocketState.Open)
                {
                    if (!await InitPhase().ConfigureAwait(false))
                    {
                        return await Disconnect().ConfigureAwait(false);
                    }

                    if (!await HelloPhase().ConfigureAwait(false))
                    {
                        return await Disconnect().ConfigureAwait(false);
                    }

                    if (!await HandshakePhase().ConfigureAwait(false))
                    {
                        return await Disconnect().ConfigureAwait(false);
                    }

                    return View("Connected", _model);
                }
                else
                {
                    return await Disconnect().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                return await Disconnect(ex.Message).ConfigureAwait(false);
            }
        }

        private async Task<bool> InitPhase()
        {
            // send init request message
            byte[] initRequest = new byte[2];
            initRequest[0] = SHIPMessageType.INIT;
            initRequest[1] = SHIPMessageValue.CMI_HEAD;

            await _wsClient.SendAsync(initRequest, WebSocketMessageType.Binary, true, new CancellationTokenSource(SHIPMessageTimeout.CMI_TIMEOUT).Token).ConfigureAwait(false);

            // wait for init response message from server
            byte[] initResponse = new byte[2];
            WebSocketReceiveResult result = await _wsClient.ReceiveAsync(initResponse, new CancellationTokenSource(SHIPMessageTimeout.CMI_TIMEOUT).Token).ConfigureAwait(false);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                return false;
            }

            if ((initResponse[0] != SHIPMessageType.INIT) || (initResponse[1] != SHIPMessageValue.CMI_HEAD))
            {
                throw new Exception("Expected init response message!");
            }

            return true;
        }

        private async Task<bool> HelloPhase()
        {
            try
            {
                // send connection data preparation ("hello" message)
                bool helloPhase = true;

                SHIPHelloMessage helloMessage = new SHIPHelloMessage();
                helloMessage.connectionHello.phase = ConnectionHelloPhaseType.ready;

                byte[] helloMessageSerialized = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(helloMessage));
                byte[] helloMessageBuffer = new byte[helloMessageSerialized.Length + 1];

                helloMessageBuffer[0] = SHIPMessageType.CONTROL;
                Buffer.BlockCopy(helloMessageSerialized, 0, helloMessageBuffer, 1, helloMessageSerialized.Length);

                await _wsClient.SendAsync(helloMessageBuffer, WebSocketMessageType.Binary, true, new CancellationTokenSource(SHIPMessageTimeout.CMI_TIMEOUT).Token).ConfigureAwait(false);

                // wait for hello response message from server
                int numProlongsReceived = 0;
                while (helloPhase)
                {
                    byte[] helloResponse = new byte[256];
                    WebSocketReceiveResult result = await _wsClient.ReceiveAsync(helloResponse, new CancellationTokenSource(SHIPMessageTimeout.T_HELLO_INIT).Token).ConfigureAwait(false);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        return false;
                    }

                    if ((result.Count < 2) || (helloResponse[0] != SHIPMessageType.CONTROL))
                    {
                        throw new Exception("Expected hello message!");
                    }

                    byte[] helloResponseMessageBuffer = new byte[result.Count - 1];
                    Buffer.BlockCopy(helloResponse, 1, helloResponseMessageBuffer, 0, result.Count - 1);

                    var settings = new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Include,
                        MissingMemberHandling = MissingMemberHandling.Error
                    };

                    SHIPHelloMessage helloMessageReceived = JsonConvert.DeserializeObject<SHIPHelloMessage>(Encoding.UTF8.GetString(helloResponseMessageBuffer), settings);
                    if (helloMessageReceived == null)
                    {
                        throw new Exception("Hello message parsing failed!");
                    }

                    switch (helloMessageReceived.connectionHello.phase)
                    {
                        case ConnectionHelloPhaseType.ready:
                            // all good, we can move on
                            helloPhase = false;
                            break;

                        case ConnectionHelloPhaseType.aborted:
                            // server aborted
                            return false;

                        case ConnectionHelloPhaseType.pending:

                            if (helloMessageReceived.connectionHello.prolongationRequestSpecified)
                            {
                                // the server needs more time, send a hello update message
                                numProlongsReceived++;
                                if (numProlongsReceived > 2)
                                {
                                    throw new Exception("More than 2 prolong requests received, aborting!");
                                }

                                await _wsClient.SendAsync(helloMessageBuffer, WebSocketMessageType.Binary, true, new CancellationTokenSource(SHIPMessageTimeout.T_HELLO_PROLONG_WAITING_GAP).Token).ConfigureAwait(false);
                            }

                            break;

                        default: throw new Exception("Invalid hello sub-state received!");
                    }
                }

                return true;
            }
            catch (Exception)
            {
                try
                {
                    // send hello abort message
                    SHIPHelloMessage helloMessage = new SHIPHelloMessage();
                    helloMessage.connectionHello.phase = ConnectionHelloPhaseType.aborted;

                    byte[] helloMessageSerialized = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(helloMessage));
                    byte[] helloMessageBuffer = new byte[helloMessageSerialized.Length + 1];

                    helloMessageBuffer[0] = SHIPMessageType.CONTROL;
                    Buffer.BlockCopy(helloMessageSerialized, 0, helloMessageBuffer, 1, helloMessageSerialized.Length);
                    
                    await _wsClient.SendAsync(helloMessageBuffer, WebSocketMessageType.Binary, true, new CancellationTokenSource(SHIPMessageTimeout.CMI_TIMEOUT).Token).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    // do nothing
                }

                throw;
            }
        }

        private async Task<bool> HandshakePhase()
        {
            try
            {
                // send protocol handshake message
                SHIPHandshakeMessage handshakeMessage = new SHIPHandshakeMessage();
                handshakeMessage.messageProtocolHandshake.handshakeType = ProtocolHandshakeTypeType.announceMax;
                handshakeMessage.messageProtocolHandshake.version = new MessageProtocolHandshakeTypeVersion
                {
                    major = 1,
                    minor = 0
                };
                handshakeMessage.messageProtocolHandshake.formats = new string[] { SHIPMessageFormat.JSON_UTF8 };

                byte[] handshakeMessageSerialized = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(handshakeMessage));
                byte[] handshakeMessageBuffer = new byte[handshakeMessageSerialized.Length + 1];

                handshakeMessageBuffer[0] = SHIPMessageType.CONTROL;
                Buffer.BlockCopy(handshakeMessageSerialized, 0, handshakeMessageBuffer, 1, handshakeMessageSerialized.Length);

                await _wsClient.SendAsync(handshakeMessageBuffer, WebSocketMessageType.Binary, true, new CancellationTokenSource(SHIPMessageTimeout.CMI_TIMEOUT).Token).ConfigureAwait(false);

                // wait for handshake response message from server
                byte[] handshakeResponse = new byte[256];
                WebSocketReceiveResult result = await _wsClient.ReceiveAsync(handshakeResponse, new CancellationTokenSource(SHIPMessageTimeout.CMI_TIMEOUT).Token).ConfigureAwait(false);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    return false;
                }

                if ((result.Count < 2) || (handshakeResponse[0] != SHIPMessageType.CONTROL))
                {
                    throw new Exception("Handshake message expected!");
                }

                byte[] handshakeResponseMessageBuffer = new byte[result.Count - 1];
                Buffer.BlockCopy(handshakeResponse, 1, handshakeResponseMessageBuffer, 0, result.Count - 1);

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Include,
                    MissingMemberHandling = MissingMemberHandling.Error
                };

                SHIPHandshakeMessage handshakeMessageReceived = JsonConvert.DeserializeObject<SHIPHandshakeMessage>(Encoding.UTF8.GetString(handshakeResponseMessageBuffer), settings);
                if (handshakeMessageReceived == null)
                {
                    throw new Exception("Handshake message parsing failed!");
                }

                if (handshakeMessageReceived.messageProtocolHandshake.handshakeType != ProtocolHandshakeTypeType.select)
                {
                    throw new Exception("Protocol version selection expected!");
                }

                if (handshakeMessageReceived.messageProtocolHandshake.version.major != 1 && handshakeMessageReceived.messageProtocolHandshake.version.minor != 0)
                {
                    throw new Exception("Protocol version mismatch!");
                }

                if ((handshakeMessageReceived.messageProtocolHandshake.formats.Length > 0) && (handshakeMessageReceived.messageProtocolHandshake.formats[0] == SHIPMessageFormat.JSON_UTF8))
                {
                    // send the message back
                    byte[] handshakeReturn = new byte[result.Count];
                    Buffer.BlockCopy(handshakeResponse, 0, handshakeReturn, 0, result.Count);
                    await _wsClient.SendAsync(handshakeReturn, WebSocketMessageType.Binary, true, new CancellationTokenSource(SHIPMessageTimeout.CMI_TIMEOUT).Token).ConfigureAwait(false);

                    return true;
                }
                else
                {
                    throw new Exception("Protocol format mismatch!");
                }
            }
            catch (Exception ex)
            {
                try
                {
                    // send handshake error message
                    SHIPHandshakeErrorMessage handshakeErrorMessage = new SHIPHandshakeErrorMessage();

                    if (ex.Message.Contains("mismatch"))
                    {
                        handshakeErrorMessage.messageProtocolHandshakeError.error = SHIPHandshakeError.SELECTION_MISMATCH;
                    }
                    else
                    {
                        handshakeErrorMessage.messageProtocolHandshakeError.error = SHIPHandshakeError.UNEXPECTED_MESSAGE;
                    }
                    
                    byte[] handshakeMessageSerialized = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(handshakeErrorMessage));
                    byte[] handshakeMessageBuffer = new byte[handshakeMessageSerialized.Length + 1];

                    handshakeMessageBuffer[0] = SHIPMessageType.CONTROL;
                    Buffer.BlockCopy(handshakeMessageSerialized, 0, handshakeMessageBuffer, 1, handshakeMessageSerialized.Length);

                    await _wsClient.SendAsync(handshakeMessageBuffer, WebSocketMessageType.Binary, true, new CancellationTokenSource(SHIPMessageTimeout.CMI_TIMEOUT).Token).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    // do nothing
                }

                throw;
            }
        }

        [HttpPost]
        public async Task<IActionResult> Disconnect(string errorMessage = null)
        {
            try
            {
                if (_wsClient != null)
                {
                    await _wsClient.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None).ConfigureAwait(false);
                    _wsClient.Dispose();
                    _wsClient = null;
                }

                if (string.IsNullOrEmpty(errorMessage))
                {
                    return View("Index", _mDNSClient.getEEBUSNodes());
                }
                else
                {
                    _model.Error = "Error: " + errorMessage;
                    return View("Index", new ServerNode[] { _model });
                }
            }
            catch (Exception)
            {
                return View("Index", _mDNSClient.getEEBUSNodes());
            }
        }
    }
}
