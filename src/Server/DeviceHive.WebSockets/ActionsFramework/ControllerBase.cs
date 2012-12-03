using System;
using System.Linq;
using DeviceHive.WebSockets.Controllers;
using DeviceHive.WebSockets.Network;
using Newtonsoft.Json.Linq;

namespace DeviceHive.WebSockets.ActionsFramework
{
    public abstract class ControllerBase
    {
        #region Private fields

        private readonly ActionInvoker _actionInvoker;
        private readonly WebSocketServerBase _server;

        #endregion

        #region Constructor

        protected ControllerBase(ActionInvoker actionInvoker, WebSocketServerBase server)
        {
            _actionInvoker = actionInvoker;
            _server = server;
        }

        #endregion

        #region Properties

        protected WebSocketServerBase Server
        {
            get { return _server; }
        }
        
        protected WebSocketConnectionBase Connection { get; private set; }

        protected string ActionName { get; private set; }

        protected JObject ActionArgs { get; private set; }

        public virtual bool IsAuthenticated
        {
            get { return true; }
        }

        #endregion

        #region Public methods

        public virtual void InvokeAction(WebSocketConnectionBase connection, string action, JObject args)
        {
            Connection = connection;
            ActionName = action;
            ActionArgs = args;

            try
            {
                _actionInvoker.InvokeAction(this, action);
            }
            catch (WebSocketRequestException e)
            {
                SendResponse(new JProperty("error", e.Message));
            }
            catch (Exception)
            {
                SendResponse(new JProperty("error", "Server error"));
                throw;
            }
        }

        #endregion

        #region Internal methods

        protected internal void InitConnection(WebSocketConnectionBase connection,
            string actionName, JObject actionArgs)
        {
            Connection = connection;
            ActionName = actionName;
            ActionArgs = actionArgs;
        }

        #endregion

        #region Protected methods

        protected void SendResponse(WebSocketConnectionBase connection, string action,
            params JProperty[] properties)
        {
            var actionProperty = new JProperty("action", action);
            var responseProperties = new object[] {actionProperty}.Concat(properties).ToArray();
            var responseObj = new JObject(responseProperties);
            connection.Send(responseObj.ToString());
        }

        protected void SendResponse(string action, params JProperty[] properties)
        {
            if (Connection != null)
                SendResponse(Connection, action, properties);
        }

        protected void SendResponse(params JProperty[] properties)
        {
            if (Connection != null)
                SendResponse(Connection, ActionName, properties);
        }
        
        protected void SendSuccessResponse()
        {
            SendResponse(new JProperty("success", "true"));
        }

        #endregion
    }
}