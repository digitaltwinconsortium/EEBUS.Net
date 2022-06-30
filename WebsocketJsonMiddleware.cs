
using EEBUS.Models;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EEBUS
{
    public class WebsocketJsonMiddleware
    {
        private readonly RequestDelegate _next;
        private ConcurrentDictionary<string, EEBUSNode> connectedNodes = new ConcurrentDictionary<string, EEBUSNode>();


        public WebsocketJsonMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            try
            {
                if (httpContext.WebSockets.IsWebSocketRequest)
                {
                    await HandleWebsockets(httpContext).ConfigureAwait(false);
                    return;
                }

                // passed on to next middleware
                await _next(httpContext).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
                Console.WriteLine(ex.StackTrace);

                httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await httpContext.Response.WriteAsync("Error while processing websocket: " + ex.Message).ConfigureAwait(false);
            }
        }

        private async Task HandleWebsockets(HttpContext httpContext)
        {
            try
            {
                string requestPath = httpContext.Request.Path.Value;
                string connectedNodeName = requestPath.Split('/').LastOrDefault();

                if (!await IsProtocolSupported(httpContext, connectedNodeName).ConfigureAwait(false))
                {
                    return;
                }

                var socket = await httpContext.WebSockets.AcceptWebSocketAsync("ship").ConfigureAwait(false);

                if (socket == null || socket.State != WebSocketState.Open)
                {
                    await _next(httpContext).ConfigureAwait(false);
                    return;
                }

                if (!connectedNodes.ContainsKey(connectedNodeName))
                {
                    connectedNodes.TryAdd(connectedNodeName, new EEBUSNode(connectedNodeName, socket));
                    Console.WriteLine($"No. of active connectedNodes : {connectedNodes.Count}");
                }
                else
                {
                    try
                    {
                        var oldSocket = connectedNodes[connectedNodeName].WebSocket;
                        connectedNodes[connectedNodeName].WebSocket = socket;
                        if (oldSocket != null)
                        {
                            Console.WriteLine($"New websocket request received for {connectedNodeName}");
                            if (oldSocket != socket && oldSocket.State != WebSocketState.Closed)
                            {
                                Console.WriteLine($"Closing old websocket for {connectedNodeName}");

                                await oldSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Client initiated new websocket!", CancellationToken.None).ConfigureAwait(false);
                            }
                        }
                        Console.WriteLine($"Websocket replaced successfully for {connectedNodeName}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Exception: " + ex.Message);
                        Console.WriteLine(ex.StackTrace);
                    }
                }

                if (socket.State == WebSocketState.Open)
                {
                    await HandleActiveConnection(socket, connectedNodeName).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }

        private async Task HandleActiveConnection(WebSocket webSocket, string connectedNodeName)
        {
            try
            {
                if (webSocket.State == WebSocketState.Open)
                {
                    await HandlePayloadsAsync(connectedNodeName, webSocket).ConfigureAwait(false);
                }

                if (webSocket.State != WebSocketState.Open && connectedNodes.ContainsKey(connectedNodeName) && connectedNodes[connectedNodeName].WebSocket == webSocket)
                {
                    await RemoveConnectionsAsync(connectedNodeName, webSocket).ConfigureAwait(false);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }

        private async Task<bool> IsProtocolSupported(HttpContext httpContext, string connectedNodeName)
        {
            string errorMessage = string.Empty;

            IList<string> requestedProtocols = httpContext.WebSockets.WebSocketRequestedProtocols;
            if (requestedProtocols.Count == 0)
            {
                errorMessage = "No protocol header message!";
            }
            else
            {
                if (!requestedProtocols.Contains("ship"))
                {
                    errorMessage = "Protocol not supported!";
                }
                else
                {
                    return true;
                }
            }

            // request for unsupported protocols are accepted and closed
            var socket = await httpContext.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false);
            await socket.CloseOutputAsync(WebSocketCloseStatus.ProtocolError, errorMessage, CancellationToken.None).ConfigureAwait(false);

            return false;
        }

        private async Task<string> ReceiveDataFromConnectedNodeAsync(WebSocket webSocket, string connectedNodeName)
        {
            try
            {
                ArraySegment<byte> data = new ArraySegment<byte>(new byte[1024]);
                WebSocketReceiveResult result;
                string payloadString = string.Empty;

                do
                {
                    result = await webSocket.ReceiveAsync(data, CancellationToken.None).ConfigureAwait(false);

                    if (result.CloseStatus.HasValue)
                    {
                        if (webSocket != connectedNodes[connectedNodeName].WebSocket)
                        {
                            if (webSocket.State != WebSocketState.CloseReceived)
                            {
                                await webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "New web request message", CancellationToken.None).ConfigureAwait(false);
                            }
                        }
                        else
                        {
                            await RemoveConnectionsAsync(connectedNodeName, webSocket).ConfigureAwait(false);
                        }

                        return null;
                    }

                    payloadString += Encoding.UTF8.GetString(data.Array, 0, result.Count);
                }
                while (!result.EndOfMessage);

                return payloadString;
            }
            catch (WebSocketException websocex)
            {
                if (webSocket != connectedNodes[connectedNodeName].WebSocket)
                {
                    Console.WriteLine($"Websocket exception occured in the old socket while receiving payload from connected node {connectedNodeName}. Error : {websocex.Message}");
                }
                else
                {
                    Console.WriteLine("Exception: " + websocex.Message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
                Console.WriteLine(ex.StackTrace);
            }

            return null;
        }

        private async Task SendPayloadToConnectedNodeAsync(string connectedNodeName, object payload, WebSocket webSocket)
        {
            var connectedNode = connectedNodes[connectedNodeName];

            try
            {
                connectedNode.WebsocketBusy = true;

                var settings = new JsonSerializerSettings { DateFormatString = "yyyy-MM-ddTHH:mm:ss.fffZ", NullValueHandling = NullValueHandling.Ignore };
                var serializedPayload = JsonConvert.SerializeObject(payload, settings);

                ArraySegment<byte> data = Encoding.UTF8.GetBytes(serializedPayload);

                if (webSocket.State == WebSocketState.Open)
                {
                    await webSocket.SendAsync(data, WebSocketMessageType.Text, true, CancellationToken.None).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
                Console.WriteLine(ex.StackTrace);
            }

            connectedNode.WebsocketBusy = false;
        }

        private JArray ProcessPayload(string payloadString, string connectedNodeName)
        {
            try
            {
                if (payloadString != null)
                {
                    var basePayload = JsonConvert.DeserializeObject<JArray>(payloadString);
                    return basePayload;
                }
                else
                {
                    Console.WriteLine($"Null payload received for {connectedNodeName}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
                Console.WriteLine(ex.StackTrace);
            }

            return null;
        }

        private async Task RemoveConnectionsAsync(string connectedNodeName, WebSocket webSocket)
        {
            try
            {
                if (connectedNodes.TryRemove(connectedNodeName, out EEBUSNode connectedNode))
                {
                    Console.WriteLine($"Removed connected node {connectedNodeName}");
                }
                else
                {
                    Console.WriteLine($"Cannot remove connected node {connectedNodeName}");
                }

                await webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Client requested closure!", CancellationToken.None).ConfigureAwait(false);
                Console.WriteLine($"Closed websocket for connectedNode {connectedNodeName}. Remaining active connectedNodes : {connectedNodes.Count}");

            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }

        private async Task HandlePayloadsAsync(string connectedNodeName, WebSocket webSocket)
        {
            while (webSocket.State == WebSocketState.Open)
            {
                try
                {
                    string payloadString = await ReceiveDataFromConnectedNodeAsync(webSocket, connectedNodeName).ConfigureAwait(false);
                    var payload = ProcessPayload(payloadString, connectedNodeName);

                    if (payload != null)
                    {
                        JArray response = null;

                        // TODO: process payload

                        if (response != null)
                        {
                            await SendPayloadToConnectedNodeAsync(connectedNodeName, response, webSocket).ConfigureAwait(false);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception: " + ex.Message);
                    Console.WriteLine(ex.StackTrace);
                }
            }
        }
    }
}
