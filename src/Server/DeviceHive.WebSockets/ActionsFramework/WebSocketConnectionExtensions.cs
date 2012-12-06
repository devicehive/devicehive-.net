using System.Collections.Generic;
using System.Linq;
using DeviceHive.WebSockets.Network;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace DeviceHive.WebSockets.ActionsFramework
{
    public static class WebSocketConnectionExtensions
    {
        public static void SendResponse(this WebSocketConnectionBase connection,
            string action, params JProperty[] properties)
        {
            var mainProperties = new List<JProperty>()
            {
                new JProperty("action", action),
            };

            var responseProperties = mainProperties.Concat(properties).Cast<object>().ToArray();
            var responseObj = new JObject(responseProperties);

            connection.Send(responseObj.ToString(Formatting.None,
                new IsoDateTimeConverter { DateTimeFormat = "yyyy-MM-ddTHH:mm:ss.ffffff" }));
        }        
    }
}