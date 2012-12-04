using System;
using System.Collections.Generic;
using System.Linq;
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
                BeforeActionInvoke();
                _actionInvoker.InvokeAction(this, action);
                AfterActionInvoke();
            }
            catch (WebSocketRequestException e)
            {
                SendErrorResponse(e.Message);
            }
            catch (Exception)
            {
                SendErrorResponse("Server error");
                throw;
            }
        }

        protected virtual void BeforeActionInvoke()
        {            
        }

        protected virtual void AfterActionInvoke()
        {            
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
            bool isErrorResponse, params JProperty[] properties)
        {
            var mainProperties = new List<JProperty>()
            {
                new JProperty("action", action),
                new JProperty("status", isErrorResponse ? "error" : "success")
            };

            if (ActionArgs != null)
            {
                var requestId = ActionArgs["requestId"];
                if (requestId != null)
                    mainProperties.Add(new JProperty("requestId", requestId));
            }

            var responseProperties = mainProperties.Concat(properties).Cast<object>().ToArray();
            var responseObj = new JObject(responseProperties);

            connection.Send(responseObj.ToString());
        }

        protected void SendResponse(WebSocketConnectionBase connection, string action,
            params JProperty[] properties)
        {
            SendResponse(connection, action, false, properties);
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
            SendResponse();
        }

        protected void SendErrorResponse(WebSocketConnectionBase connection, string action, string error)
        {
            SendResponse(connection, action, true, new JProperty("error", error));
        }

        protected void SendErrorResponse(string error)
        {
            SendErrorResponse(Connection, ActionName, error);
        }

        #endregion
    }
}