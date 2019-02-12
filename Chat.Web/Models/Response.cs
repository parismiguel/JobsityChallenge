using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using static Chat.Web.Models.Enums;

namespace Chat.Web.Models
{
    public class Response
    {
        public Response()
        {
            Status = false;
            Type = ResponseType.error;
        }

        public bool Status { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
        public string Trace { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public ResponseType Type { get; set; }
    }
}
