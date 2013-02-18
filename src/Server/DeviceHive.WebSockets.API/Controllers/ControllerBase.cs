using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
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

        private readonly IJsonMapper<DeviceCommand> _commandMapper;
        private readonly IJsonMapper<DeviceNotification> _notificationMapper;

        #endregion

        #region Constructor

        protected ControllerBase(ActionInvoker actionInvoker,
            DataContext dataContext, JsonMapperManager jsonMapperManager) :
            base(actionInvoker)
        {
            _dataContext = dataContext;
        
            _commandMapper = jsonMapperManager.GetMapper<DeviceCommand>();
            _notificationMapper = jsonMapperManager.GetMapper<DeviceNotification>();
        }

        #endregion

        #region Properties

        protected DataContext DataContext
        {
            get { return _dataContext; }
        }

        protected IJsonMapper<DeviceCommand> CommandMapper
        {
            get { return _commandMapper; }
        }

        protected IJsonMapper<DeviceNotification> NotificationMapper
        {
            get { return _notificationMapper; }
        }

        #endregion

        #region Protected methods

        protected void Validate(object entity)
        {
            var result = new List<ValidationResult>();
            if (!Validator.TryValidateObject(entity, new ValidationContext(entity, null, null), result, true))
                throw new WebSocketRequestException(result.First().ErrorMessage);
        }

        #endregion
    }
}