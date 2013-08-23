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
        private readonly JsonMapperManager _jsonMapperManager;

        #endregion

        #region Constructor

        protected ControllerBase(ActionInvoker actionInvoker, DataContext dataContext, JsonMapperManager jsonMapperManager) :
            base(actionInvoker)
        {
            _dataContext = dataContext;
            _jsonMapperManager = jsonMapperManager;
        }

        #endregion

        #region Public Properties

        public DataContext DataContext
        {
            get { return _dataContext; }
        }

        #endregion

        #region Protected methods

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

        #endregion
    }
}