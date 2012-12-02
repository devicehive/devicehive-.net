using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Description;
using DeviceHive.API.Filters;
using Newtonsoft.Json.Linq;

namespace DeviceHive.API.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class CronController : BaseController
    {
        [HttpGet]
        [HttpNoContentResponse]
        public void RefreshDeviceStatus()
        {
            var devices = DataContext.Device.GetOfflineDevices();
            foreach (var device in devices)
            {
                device.Status = "Offline";
                DataContext.Device.Save(device);
            }
        }
    }
}