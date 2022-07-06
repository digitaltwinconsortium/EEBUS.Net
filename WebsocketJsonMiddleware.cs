
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

                await SendAndReceive(connectedNodeName, socket).ConfigureAwait(false);
                                
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

        private async Task SendAndReceive(string connectedNodeName, WebSocket webSocket)
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
                    byte[] messageBuffer = new byte[result.Count - 1];
                    Buffer.BlockCopy(receiveBuffer, 1, messageBuffer, 0, result.Count - 1);

                    // setup JSON serializer settings
                    JsonSerializerSettings jsonSettings = new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Include,
                        MissingMemberHandling = MissingMemberHandling.Error
                    };

                    switch (receiveBuffer[0])
                    {
                        case SHIPMessageType.INIT:

                            Console.WriteLine($"Init message received from {connectedNodeName}.");

                            if (messageBuffer[0] != SHIPMessageValue.CMI_HEAD)
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

                            // there are 6 control messages defined: Hello, ProtocolHandshake, ProtocolHandShakeError, AccessMethodsRequest, PINVerification and PINVerificationError
                            // Note: We ignore PINVerification and PINVerificationError messages
                            bool controlMessageHandled = false;

                            SHIPHelloMessage helloMessageReceived = null;
                            try
                            {
                                helloMessageReceived = JsonConvert.DeserializeObject<SHIPHelloMessage>(Encoding.UTF8.GetString(messageBuffer), jsonSettings);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Exception: " + ex.Message);
                            }

                            if ((helloMessageReceived != null) && (helloMessageReceived.connectionHello != null))
                            {
                                Console.WriteLine($"Hello message received from {connectedNodeName}.");

                                if (!await HandleHelloMessage(webSocket, helloMessageReceived.connectionHello).ConfigureAwait(false))
                                {
                                    throw new Exception("Hello aborted!");
                                }

                                controlMessageHandled = true;
                            }

                            SHIPHandshakeMessage handshakeMessageReceived = null;
                            try
                            {
                                handshakeMessageReceived = JsonConvert.DeserializeObject<SHIPHandshakeMessage>(Encoding.UTF8.GetString(messageBuffer), jsonSettings);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Exception: " + ex.Message);
                            }

                            if ((handshakeMessageReceived != null) && (handshakeMessageReceived.messageProtocolHandshake != null))
                            {
                                Console.WriteLine($"Handshake message received from {connectedNodeName}.");

                                if (!await HandleHandshakeMessage(webSocket, handshakeMessageReceived.messageProtocolHandshake).ConfigureAwait(false))
                                {
                                    throw new Exception("Handshake aborted!");
                                }

                                controlMessageHandled = true;
                            }

                            SHIPHandshakeErrorMessage handshakeErrorMessageReceived = null;
                            try
                            {
                                handshakeErrorMessageReceived = JsonConvert.DeserializeObject<SHIPHandshakeErrorMessage>(Encoding.UTF8.GetString(messageBuffer), jsonSettings);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Exception: " + ex.Message);
                            }

                            if ((handshakeErrorMessageReceived != null) && (handshakeErrorMessageReceived.messageProtocolHandshakeError != null))
                            {
                                Console.WriteLine($"Handshake error message received from {connectedNodeName} due to {handshakeErrorMessageReceived.messageProtocolHandshakeError.error}.");

                                controlMessageHandled = true;

                                throw new Exception("Handshake aborted!");
                            }

                            SHIPAccessMethodsMessage accessMethodsMessageReceived = null;
                            try
                            {
                                accessMethodsMessageReceived = JsonConvert.DeserializeObject<SHIPAccessMethodsMessage>(Encoding.UTF8.GetString(messageBuffer), jsonSettings);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Exception: " + ex.Message);
                            }

                            if ((accessMethodsMessageReceived != null) && (accessMethodsMessageReceived.accessMethodsRequest != null))
                            {
                                Console.WriteLine($"Access Methods message received from {connectedNodeName}.");

                                if (!HandleAccessMethodsMessage(connectedNodeName, webSocket, accessMethodsMessageReceived.accessMethodsRequest))
                                {
                                    throw new Exception("Access methods received message aborted!");
                                }

                                controlMessageHandled = true;
                            }

                            if (!controlMessageHandled)
                            {
                                Console.WriteLine($"Control message from {connectedNodeName} ignored!");
                            }

                            break;

                        case SHIPMessageType.DATA:

                            Console.WriteLine($"Data message received from {connectedNodeName}.");

                            SHIPDataMessage dataMessageReceived = JsonConvert.DeserializeObject<SHIPDataMessage>(Encoding.UTF8.GetString(messageBuffer), jsonSettings);
                            if ((dataMessageReceived != null) && (dataMessageReceived.data != null))
                            {
                                if (!await HandleDataMessage(webSocket, dataMessageReceived.data).ConfigureAwait(false))
                                {
                                    throw new Exception("Data message aborted!");
                                }
                            }

                            break;

                        case SHIPMessageType.END:

                            Console.WriteLine($"Close message received from {connectedNodeName}.");

                            SHIPCloseMessage closeMessageReceived = JsonConvert.DeserializeObject<SHIPCloseMessage>(Encoding.UTF8.GetString(messageBuffer), jsonSettings);
                            if ((closeMessageReceived != null) && (closeMessageReceived.connectionClose != null))
                            {
                                if (!await HandleCloseMessage(webSocket, closeMessageReceived.connectionClose).ConfigureAwait(false))
                                {
                                    throw new Exception("Close message aborted!");
                                }
                            }
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
                catch (Exception innerEx)
                {
                    Console.WriteLine("Exception: " + innerEx.Message);
                }

                throw;
            }
        }

        private bool HandleAccessMethodsMessage(string connectedNodeName, WebSocket webSocket, AccessMethodsType accessMethods)
        {
            try
            {
                // simply print the access methods to the console
                if (accessMethods.dnsSd_mDns != null)
                {
                    Console.WriteLine($"Received access method mDNS from {connectedNodeName} with ID {accessMethods.id} at {accessMethods.dns.uri}.");
                }

                if (accessMethods.dns != null)
                {
                    Console.WriteLine($"Received access method DNS from {connectedNodeName} with ID {accessMethods.id} at {accessMethods.dns.uri}.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
            }
                
            return true;
        }

        private async Task<bool> HandleDataMessage(WebSocket webSocket, DataType data)
        {
            try
            {
                if (data.header.protocolId != "spine")
                {
                    throw new Exception("SPINE protocol expected!");
                }

                // handle SPINE payload
                if (!await HandleSpineMessage(webSocket, data.payload).ConfigureAwait(false))
                {
                    throw new Exception("Handle SPINE message failed!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
            }

            return true;
        }

        public async Task<bool> SendDataMessage(WebSocket webSocket, object payload)
        {
            try
            {
                // send data payload
                SHIPDataMessage dataMessage = new SHIPDataMessage();
                dataMessage.data.payload = payload;

                byte[] dataMessageSerialized = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(dataMessage));
                byte[] dataMessageBuffer = new byte[dataMessageSerialized.Length + 1];

                dataMessageBuffer[0] = SHIPMessageType.DATA;
                Buffer.BlockCopy(dataMessageSerialized, 0, dataMessageBuffer, 1, dataMessageSerialized.Length);

                await webSocket.SendAsync(dataMessageBuffer, WebSocketMessageType.Binary, true, new CancellationTokenSource(SHIPMessageTimeout.CMI_TIMEOUT).Token).ConfigureAwait(false);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
                return false;
            }
        }

        private async Task<bool> HandleSpineMessage(WebSocket webSocket, object payload)
        {
            // sinply print the SPINE payload to the console for now
            Console.WriteLine($"SPINE data received: {payload}.");

            // send the same message back for now
            return await SendDataMessage(webSocket, payload).ConfigureAwait(false);
        }

        private async Task<bool> HandleCloseMessage(WebSocket webSocket, ConnectionCloseType connectionClose)
        {
            try
            {
                if (connectionClose.phase != ConnectionClosePhaseType.announce)
                {
                    throw new Exception("Close connection announcement expected!");
                }

                if (connectionClose.reasonSpecified && connectionClose.reason == ConnectionCloseReasonType.removedConnection)
                {
                    Console.WriteLine($"Received close connection request with removed connection reason");
                }

                if (connectionClose.reasonSpecified && connectionClose.reason == ConnectionCloseReasonType.unspecific)
                {
                    Console.WriteLine($"Received close connection request with unspecific reason");
                }

                // send confirmation back
                SHIPCloseMessage closeMessage = new SHIPCloseMessage();
                closeMessage.connectionClose.phase = ConnectionClosePhaseType.confirm;

                byte[] closeMessageSerialized = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(closeMessage));
                byte[] closeMessageBuffer = new byte[closeMessageSerialized.Length + 1];

                closeMessageBuffer[0] = SHIPMessageType.END;
                Buffer.BlockCopy(closeMessageSerialized, 0, closeMessageBuffer, 1, closeMessageSerialized.Length);

                await webSocket.SendAsync(closeMessageBuffer, WebSocketMessageType.Binary, true, new CancellationTokenSource(SHIPMessageTimeout.CMI_TIMEOUT).Token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
            }

            return true;
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
