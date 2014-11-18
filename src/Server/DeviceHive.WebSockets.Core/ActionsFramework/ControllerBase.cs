using System;
using System.Collections.Generic;
using System.Linq;
using DeviceHive.WebSockets.Core.Network;
using Newtonsoft.Json.Linq;
using log4net;

namespace DeviceHive.WebSockets.Core.ActionsFramework
{
    public abstract class ControllerBase
    {
        #region Private Fields

        private readonly ILog _logger;
        private readonly ActionInvoker _actionInvoker;

        #endregion

        #region Constructor

        protected ControllerBase(ActionInvoker actionInvoker)
        {
            _logger = LogManager.GetLogger(GetType());
            _actionInvoker = actionInvoker;
        }

        #endregion

        #region Protected Properties

        protected ActionContext ActionContext { get; private set; }

        protected WebSocketConnectionBase Connection
        {
            get { return ActionContext.Connection; }
        }

        protected JObject Request
        {
            get { return ActionContext.Request; }
        }

        #endregion

        #region Public Methods

        public void InvokeAction(ActionContext actionContext)
        {
            if (actionContext == null)
                throw new ArgumentNullException("actionContext");

            ActionContext = actionContext;

            _logger.DebugFormat("Invoking action {0} on connection: {1}", ActionContext.Action, Connection.Identity);
            try
            {
                _actionInvoker.InvokeAction(ActionContext);
            }
            catch (WebSocketRequestException e)
            {
                _logger.Debug("Action resulted in exception, sending error response", e);
                SendErrorResponse(e.Message);
            }
            catch (Exception)
            {
                SendErrorResponse("Server error");
                throw;
            }
        }

        public void InvokePingAction(ActionContext actionContext)
        {
            if (actionContext == null)
                throw new ArgumentNullException("actionContext");

            ActionContext = actionContext;

            _logger.DebugFormat("Invoking ping on connection: {0}", Connection.Identity);
            _actionInvoker.InvokePingAction(ActionContext);
        }

        public virtual void CleanupConnection(WebSocketConnectionBase connection)
        {
        }

        #endregion

        #region Send Response Helpers

        private JProperty[] GetResponseProperties(bool isErrorResponse, params JProperty[] baseProperties)
        {
            var additionalProperties = new List<JProperty>()
            {
                new JProperty("status", isErrorResponse ? "error" : "success")
            };

            if (ActionContext.Request != null)
            {
                var requestId = ActionContext.Request["requestId"];
                if (requestId != null)
                    additionalProperties.Add(new JProperty("requestId", requestId));
            }

            return additionalProperties.Concat(baseProperties).ToArray();
        }
        
        protected void SendResponse(params JProperty[] properties)
        {
            Connection.SendResponse(ActionContext.Action, GetResponseProperties(false, properties));
        }
        
        protected void SendSuccessResponse()
        {
            SendResponse();
        }

        protected void SendErrorResponse(string error)
        {
            Connection.SendResponse(ActionContext.Action, GetResponseProperties(true,
                new[] {new JProperty("error", error)}));
        }

        #endregion
    }
}