
using EEBUS.Enums;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EEBUS
{
    public class WebsocketJsonMiddleware
    {
        private readonly RequestDelegate _next;
        private ConcurrentDictionary<string, WebSocket> connectedNodes = new ConcurrentDictionary<string, WebSocket>();


        public WebsocketJsonMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            try
            {
                if (!httpContext.WebSockets.IsWebSocketRequest)
                {
                    // passed on to next middleware
                    await _next(httpContext).ConfigureAwait(false);
                }

                if (!ProtocolSupported(httpContext))
                {
                    // passed on to next middleware
                    await _next(httpContext).ConfigureAwait(false);
                }

                string connectedNodeName = httpContext.Request.Host.Host;
                if (connectedNodes.ContainsKey(connectedNodeName))
                {
                    // we only allow 1 connection per host, so close any existing ones
                    WebSocket existingSocket = connectedNodes[connectedNodeName];
                    if (existingSocket != null)
                    {
                        Console.WriteLine($"New websocket request received for existing connection {connectedNodeName}, closing old websocket!");
                        await CloseConnectionAsync(connectedNodeName, existingSocket).ConfigureAwait(false);
                    }
                }

                var socket = await httpContext.WebSockets.AcceptWebSocketAsync("ship").ConfigureAwait(false);
                if (socket == null || socket.State != WebSocketState.Open)
                {
                    Console.WriteLine("Failed to accept socket from " + httpContext.Request.Host.Host);
                    return;
                }

                connectedNodes.TryAdd(connectedNodeName, socket);
                Console.WriteLine($"Now connected to {connectedNodeName}. Number of active connections: {connectedNodes.Count}");

                await SendAndReceive(socket).ConfigureAwait(false);
                                
                // we're done, close and return
                await CloseConnectionAsync(connectedNodeName, socket).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);

                httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await httpContext.Response.WriteAsync("Error while processing websocket: " + ex.Message).ConfigureAwait(false);
            }
        }

        private bool ProtocolSupported(HttpContext httpContext)
        {
            IList<string> requestedProtocols = httpContext.WebSockets.WebSocketRequestedProtocols;
            if ((requestedProtocols.Count == 0) || !requestedProtocols.Contains("ship"))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        private async Task SendAndReceive(WebSocket webSocket)
        {
            try
            {
                while (webSocket.State == WebSocketState.Open)
                {
                    byte[] receiveBuffer = new byte[1024];
                    WebSocketReceiveResult result = await webSocket.ReceiveAsync(receiveBuffer, new CancellationTokenSource(SHIPMessageTimeout.CMI_TIMEOUT).Token).ConfigureAwait(false);
                    if (result.CloseStatus.HasValue)
                    {
                        // close received
                        break;
                    }
                    if (result.Count < 2)
                    {
                        throw new Exception("Invalid EEBUS payload received, expected message size of at least 2!");
                    }

                    // parse EEBUS payload
                    var settings = new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Include,
                        MissingMemberHandling = MissingMemberHandling.Error
                    };

                    switch (receiveBuffer[0])
                    {
                        case SHIPMessageType.INIT:
                            if (receiveBuffer[1] != SHIPMessageValue.CMI_HEAD)
                            {
                                throw new Exception("Expected SMI_HEAD payload in INIT message!");
                            }

                            // set response payload
                            byte[] responseBuffer = new byte[2];
                            responseBuffer[0] = SHIPMessageType.INIT;
                            responseBuffer[1] = SHIPMessageValue.CMI_HEAD;

                            // send response
                            await webSocket.SendAsync(responseBuffer, WebSocketMessageType.Binary, true, new CancellationTokenSource(SHIPMessageTimeout.CMI_TIMEOUT).Token).ConfigureAwait(false);

                            break;

                        case SHIPMessageType.CONTROL:
                            byte[] controlMessageBuffer = new byte[result.Count - 1];
                            Buffer.BlockCopy(receiveBuffer, 1, controlMessageBuffer, 0, result.Count - 1);

                            // there are 5 control messages defined: Hello, ProtocolHandshake, ProtocolHandShakeError, PINVerification and PINVerificationError
                            // we ignore PINVerification and PINVerificationError messages

                            SHIPHelloMessage helloMessageReceived = null;
                            try
                            {
                                helloMessageReceived = JsonConvert.DeserializeObject<SHIPHelloMessage>(Encoding.UTF8.GetString(controlMessageBuffer), settings);
                            }
                            catch (Exception)
                            {
                                // do nothing
                            }

                            if ((helloMessageReceived != null) && (helloMessageReceived.connectionHello != null))
                            {
                                if (!await HandleHelloMessage(webSocket, helloMessageReceived.connectionHello).ConfigureAwait(false))
                                {
                                    throw new Exception("Hello phase aborted!");
                                }
                            }

                            SHIPHandshakeMessage handshakeMessageReceived = null;
                            try
                            {
                                handshakeMessageReceived = JsonConvert.DeserializeObject<SHIPHandshakeMessage>(Encoding.UTF8.GetString(controlMessageBuffer), settings);
                            }
                            catch (Exception)
                            {
                                // do nothing
                            }

                            if ((handshakeMessageReceived != null) && (handshakeMessageReceived.messageProtocolHandshake != null))
                            {
                                if (!await HandleHandshakeMessage(webSocket, handshakeMessageReceived.messageProtocolHandshake).ConfigureAwait(false))
                                {
                                    throw new Exception("Handshake phase aborted!");
                                }
                            }

                            break;

                        case SHIPMessageType.DATA:
                            break;

                        case SHIPMessageType.END:
                            break;

                        default:
                            throw new Exception("Invalid EEBUS message type received!");
                    }
                 }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
            }
        }

        private async Task<bool> HandleHelloMessage(WebSocket webSocket, ConnectionHelloType helloMessageReceived)
        {
            SHIPHelloMessage helloMessage = new SHIPHelloMessage();
            helloMessage.connectionHello.phase = ConnectionHelloPhaseType.ready;
            byte[] helloMessageSerialized = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(helloMessage));
            byte[] helloMessageBuffer = new byte[helloMessageSerialized.Length + 1];
            helloMessageBuffer[0] = SHIPMessageType.CONTROL;
            Buffer.BlockCopy(helloMessageSerialized, 0, helloMessageBuffer, 1, helloMessageSerialized.Length);

            int numProlongsReceived = 0;
            while (true)
            {
                switch (helloMessageReceived.phase)
                {
                    case ConnectionHelloPhaseType.ready:
                        // send "ready" message back
                        await webSocket.SendAsync(helloMessageBuffer, WebSocketMessageType.Binary, true, new CancellationTokenSource(SHIPMessageTimeout.CMI_TIMEOUT).Token).ConfigureAwait(false);

                        // all good, we can move on
                        return true;

                    case ConnectionHelloPhaseType.aborted:
                        // client aborted
                        return false;

                    case ConnectionHelloPhaseType.pending:
                        if (helloMessageReceived.prolongationRequestSpecified)
                        {
                            // the client needs more time, send a hello update message
                            numProlongsReceived++;

                            if (numProlongsReceived > 2)
                            {
                                Console.WriteLine("More than 2 prolong requests received, aborting!");
                                helloMessage.connectionHello.phase = ConnectionHelloPhaseType.aborted;
                                helloMessageSerialized = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(helloMessage));
                                helloMessageBuffer = new byte[helloMessageSerialized.Length + 1];
                                Buffer.BlockCopy(helloMessageSerialized, 0, helloMessageBuffer, 1, helloMessageSerialized.Length);

                                // send "abort" message
                                await webSocket.SendAsync(helloMessageBuffer, WebSocketMessageType.Binary, true, new CancellationTokenSource(SHIPMessageTimeout.CMI_TIMEOUT).Token).ConfigureAwait(false);

                                return false;
                            }

                            // send "ready" message
                            await webSocket.SendAsync(helloMessageBuffer, WebSocketMessageType.Binary, true, new CancellationTokenSource(SHIPMessageTimeout.CMI_TIMEOUT).Token).ConfigureAwait(false);
                        }
                        else
                        {
                            // send "ready" message
                            await webSocket.SendAsync(helloMessageBuffer, WebSocketMessageType.Binary, true, new CancellationTokenSource(SHIPMessageTimeout.CMI_TIMEOUT).Token).ConfigureAwait(false);
                        }

                        break;

                    default: throw new Exception("Invalid hello sub-state received!");
                }

                // receive the next hello message
                byte[] receiveBuffer = new byte[1024];
                WebSocketReceiveResult result = await webSocket.ReceiveAsync(receiveBuffer, new CancellationTokenSource(SHIPMessageTimeout.CMI_TIMEOUT).Token).ConfigureAwait(false);
                if (result.CloseStatus.HasValue)
                {
                    // close received
                    return false;
                }
                if (result.Count < 2)
                {
                    throw new Exception("Invalid EEBUS payload received, expected message size of at least 2!");
                }

                // parse EEBUS payload
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Include,
                    MissingMemberHandling = MissingMemberHandling.Error
                };

                byte[] controlMessageBuffer = new byte[result.Count - 1];
                Buffer.BlockCopy(receiveBuffer, 1, controlMessageBuffer, 0, result.Count - 1);

                helloMessageReceived = JsonConvert.DeserializeObject<SHIPHelloMessage>(Encoding.UTF8.GetString(controlMessageBuffer), settings).connectionHello;
               
            }
        }

        private async Task<bool> HandleHandshakeMessage(WebSocket webSocket, MessageProtocolHandshakeType handshakeMessageReceived)
        {
            try
            {
                if (handshakeMessageReceived.handshakeType != ProtocolHandshakeTypeType.announceMax)
                {
                    throw new Exception("Protocol version max announcement expected!");
                }

                if (handshakeMessageReceived.version.major != 1 && handshakeMessageReceived.version.minor != 0)
                {
                    throw new Exception("Protocol version mismatch!");
                }

                if ((handshakeMessageReceived.formats.Length > 0) && (handshakeMessageReceived.formats[0] == SHIPMessageFormat.JSON_UTF8))
                {
                    // send protocol handshake response message
                    SHIPHandshakeMessage handshakeMessage = new SHIPHandshakeMessage();
                    handshakeMessage.messageProtocolHandshake.handshakeType = ProtocolHandshakeTypeType.select;
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

                    await webSocket.SendAsync(handshakeMessageBuffer, WebSocketMessageType.Binary, true, new CancellationTokenSource(SHIPMessageTimeout.CMI_TIMEOUT).Token).ConfigureAwait(false);

                    // wait for final confirmation from client
                    byte[] receiveBuffer = new byte[1024];
                    WebSocketReceiveResult result = await webSocket.ReceiveAsync(receiveBuffer, new CancellationTokenSource(SHIPMessageTimeout.CMI_TIMEOUT).Token).ConfigureAwait(false);
                    if (result.CloseStatus.HasValue)
                    {
                        // close received
                        return false;
                    }

                    if (handshakeMessageBuffer.Length != result.Count)
                    {
                        return false;
                    }

                    // verify that we got our selection back
                    for (int i = 0; i < handshakeMessageBuffer.Length; i++)
                    {
                        if (handshakeMessageBuffer[i] != receiveBuffer[i])
                        {
                            return false;
                        }
                    }

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
                    SHIPHandshakeErrorMessage handshakeErrorMessage = new SHIPHandshakeErrorMessage();

                    if (ex.Message.Contains("mismatch"))
                    {
                        handshakeErrorMessage.messageProtocolHandshakeError.error = SHIPHandshakeError.SELECTION_MISMATCH;
                    }
                    else
                    {
                        handshakeErrorMessage.messageProtocolHandshakeError.error = SHIPHandshakeError.UNEXPECTED_MESSAGE;
                    }

                    byte[] handshakeErrorMessageSerialized = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(handshakeErrorMessage));
                    byte[] handshakeErrorMessageBuffer = new byte[handshakeErrorMessageSerialized.Length + 1];

                    handshakeErrorMessageBuffer[0] = SHIPMessageType.CONTROL;
                    Buffer.BlockCopy(handshakeErrorMessageSerialized, 0, handshakeErrorMessageBuffer, 1, handshakeErrorMessageSerialized.Length);

                    await webSocket.SendAsync(handshakeErrorMessageBuffer, WebSocketMessageType.Binary, true, new CancellationTokenSource(SHIPMessageTimeout.CMI_TIMEOUT).Token).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    // do nothing
                }

                throw;
            }
        }

        private async Task CloseConnectionAsync(string connectedNodeName, WebSocket webSocket)
        {
            try
            {
                await webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Closing!", CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
            }

            connectedNodes.TryRemove(connectedNodeName, out _);

            Console.WriteLine($"Closed websocket for connectedNode {connectedNodeName}. Remaining active connectedNodes : {connectedNodes.Count}");
        }
    }
}
