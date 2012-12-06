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
            get { return false; }
        }

        #endregion

        #region Public methods

        public void InvokeAction(WebSocketConnectionBase connection, string action, JObject args)
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

        public TArg GetArgument<TArg>(string name)
        {
            if (ActionArgs == null)
                return default(TArg);

            var val = ActionArgs[name];
            if (val == null)
                return default(TArg);

            try
            {
                return val.ToObject<TArg>();
            }
            catch (FormatException)
            {
                throw new WebSocketRequestException("Invalid format for parameter " + name);
            }
        }


        public virtual void CleanupConnection(WebSocketConnectionBase connection)
        {            
        }

        protected virtual void BeforeActionInvoke()
        {            
        }

        protected virtual void AfterActionInvoke()
        {            
        }

        #endregion

        #region Send responce helpers

        private JProperty[] GetResponseProperties(bool isErrorResponse, params JProperty[] baseProperties)
        {
            var additionalProperties = new List<JProperty>()
            {
                new JProperty("status", isErrorResponse ? "error" : "success")
            };

            if (ActionArgs != null)
            {
                var requestId = ActionArgs["requestId"];
                if (requestId != null)
                    additionalProperties.Add(new JProperty("requestId", requestId));
            }

            return additionalProperties.Concat(baseProperties).ToArray();
        }
        
        protected void SendResponse(params JProperty[] properties)
        {
            Connection.SendResponse(ActionName, GetResponseProperties(false, properties));
        }
        
        protected void SendSuccessResponse()
        {
            SendResponse();
        }

        protected void SendErrorResponse(string error)
        {
            Connection.SendResponse(ActionName, GetResponseProperties(true,
                new[] {new JProperty("error", error)}));
        }

        #endregion
    }
}