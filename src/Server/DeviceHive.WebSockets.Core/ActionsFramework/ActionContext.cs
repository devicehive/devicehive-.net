using System;
using System.Collections.Generic;
using System.Linq;
using DeviceHive.WebSockets.Core.Network;
using Newtonsoft.Json.Linq;

namespace DeviceHive.WebSockets.Core.ActionsFramework
{
    public class ActionContext
    {
        #region Public Properties

        public WebSocketConnectionBase Connection { get; private set; }

        public ControllerBase Controller { get; private set; }
        
        public string Action { get; private set; }
        
        public JObject Request { get; private set; }

        public Dictionary<string, object> Parameters { get; private set; }

        #endregion

        #region Constructor

        public ActionContext(WebSocketConnectionBase connection, ControllerBase controller, string action, JObject request)
        {
            Connection = connection;
            Controller = controller;
            Action = action;
            Request = request;
            Parameters = new Dictionary<string, object>();
        }
        #endregion

        #region Public Methods

        public T GetRequestParameter<T>(string name)
        {
            if (Request == null)
                return default(T);

            var value = Request[name];
            if (value == null)
                return default(T);

            try
            {
                return value.ToObject<T>();
            }
            catch (FormatException)
            {
                throw new WebSocketRequestException("Invalid format for parameter " + name);
            }
        }

        public object GetParameter(string name)
        {
            object value;
            return Parameters.TryGetValue(name, out value) ? value : null;
        }
        #endregion
    }
}
