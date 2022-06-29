
using Newtonsoft.Json.Linq;

namespace EEBUS.Models
{
    public class BasePayload
    {
        public int MessageTypeId { get; set; }

        public string UniqueId { get; set; }

        public JObject Payload { get; set; }

        public JArray WrappedPayload { get; set; }
    }
}
