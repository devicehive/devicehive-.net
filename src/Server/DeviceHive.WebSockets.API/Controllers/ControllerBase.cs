using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Newtonsoft.Json.Linq;
using DeviceHive.Core;
using DeviceHive.Core.Mapping;
using DeviceHive.Data;
using DeviceHive.Data.Model;
using DeviceHive.WebSockets.Core.ActionsFramework;

namespace DeviceHive.WebSockets.API.Controllers
{
    public abstract class ControllerBase : WebSockets.Core.ActionsFramework.ControllerBase
    {
        #region Private Fields

        private readonly DataContext _dataContext;
        private readonly JsonMapperManager _jsonMapperManager;
        private readonly DeviceHiveConfiguration _deviceHiveConfiguration;

        #endregion

        #region Constructor

        protected ControllerBase(ActionInvoker actionInvoker, DataContext dataContext, JsonMapperManager jsonMapperManager, DeviceHiveConfiguration deviceHiveConfiguration) :
            base(actionInvoker)
        {
            _dataContext = dataContext;
            _jsonMapperManager = jsonMapperManager;
            _deviceHiveConfiguration = deviceHiveConfiguration;
        }

        #endregion

        #region Public Properties

        public DataContext DataContext
        {
            get { return _dataContext; }
        }

        public DeviceHiveConfiguration DeviceHiveConfiguration
        {
            get { return _deviceHiveConfiguration; }
        }

        #endregion

        #region Protected Methods

        protected void Validate(object entity)
        {
            var result = new List<ValidationResult>();
            if (!Validator.TryValidateObject(entity, new ValidationContext(entity, null, null), result, true))
                throw new WebSocketRequestException(result.First().ErrorMessage);
        }

        protected IJsonMapper<T> GetMapper<T>()
        {
            return _jsonMapperManager.GetMapper<T>();
        }

        public JObject MapDeviceNotification(DeviceNotification notification, Device device = null)
        {
            var json = GetMapper<DeviceNotification>().Map(notification);
            if (notification.Device != null)
                json["deviceGuid"] = notification.Device.GUID;
            else if (device != null)
                json["deviceGuid"] = device.GUID;
            return json;
        }

        public JObject MapDeviceCommand(DeviceCommand command, Device device = null)
        {
            var json = GetMapper<DeviceCommand>().Map(command);
            if (command.Device != null)
                json["deviceGuid"] = command.Device.GUID;
            else if (device != null)
                json["deviceGuid"] = device.GUID;
            return json;
        }

        #endregion
    }
}