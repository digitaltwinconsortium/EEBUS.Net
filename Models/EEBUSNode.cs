

using System.Net.WebSockets;
using System.Collections.Generic;

namespace EEBUS.Models
{
    public class EEBUSNode
    {
        public string Name { get; set; }

        public Dictionary<string, string> Request { get; set; }

        public Dictionary<string,object> Response { get; set; }

        public WebSocket WebSocket { get; set; }

        public bool WebsocketBusy { get; set; }

        public bool Authorized { get; set; }

        public bool WaitingResponse => Request.Count != 0;

        public EEBUSNode(string name,WebSocket webSocket)
        {
            Name = name;
            WebSocket = webSocket;
            Request = new Dictionary<string, string>();
            Response = new Dictionary<string, object>();
            WebsocketBusy = false;
            Authorized=false;
        }
    }
}
