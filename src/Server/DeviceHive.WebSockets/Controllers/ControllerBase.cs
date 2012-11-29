using System;
using System.Collections.Generic;
using System.Linq;
using DeviceHive.Core;
using DeviceHive.Core.Mapping;
using DeviceHive.Data.Model;
using DeviceHive.WebSockets.Network;
using Newtonsoft.Json.Linq;

namespace DeviceHive.WebSockets.Controllers
{
	public abstract class ControllerBase
	{
		#region Private fields

		private readonly DataContext _dataContext;
		private readonly WebSocketServerBase _server;

		private readonly IJsonMapper<DeviceCommand> _commandMapper;
		private readonly IJsonMapper<DeviceNotification> _notificationMapper;

		#endregion

		#region Constructor

		protected ControllerBase(DataContext dataContext, WebSocketServerBase server,
			JsonMapperManager jsonMapperManager)
		{
			_dataContext = dataContext;
			_server = server;

			_commandMapper = jsonMapperManager.GetMapper<DeviceCommand>();
			_notificationMapper = jsonMapperManager.GetMapper<DeviceNotification>();
		}

		#endregion

		#region Properties

		protected DataContext DataContext
		{
			get { return _dataContext; }
		}

		protected WebSocketServerBase Server
		{
			get { return _server; }
		}

		protected IJsonMapper<DeviceCommand> CommandMapper
		{
			get { return _commandMapper; }
		}

		protected IJsonMapper<DeviceNotification> NotificationMapper
		{
			get { return _notificationMapper; }
		}

		protected WebSocketConnectionBase Connection { get; private set; }

		protected string ActionName { get; private set; }

		protected JObject ActionArgs { get; private set; }

		#endregion

		#region Public methods

		public virtual void InvokeAction(WebSocketConnectionBase connection, string action, JObject args)
		{
			Connection = connection;
			ActionName = action;
			ActionArgs = args;

			try
			{
				InvokeActionImpl();
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

		#region Protected methods

		protected abstract void InvokeActionImpl();

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

		protected IEnumerable<Guid?> ParseDeviceGuids()
		{
			if (ActionArgs == null)
				return new Guid?[] {null};

			var deviceGuids = ActionArgs["deviceGuids"];
			if (deviceGuids == null)
				return new Guid?[] {null};

			var deviceGuidsArray = deviceGuids as JArray;
			if (deviceGuidsArray != null)
				return deviceGuidsArray.Select(t => (Guid?) Guid.Parse((string) t)).ToArray();

			return new Guid?[] {Guid.Parse((string) deviceGuids)};
		}

		#endregion
	}
}