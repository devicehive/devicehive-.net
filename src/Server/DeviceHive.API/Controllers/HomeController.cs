using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http.Description;
using DeviceHive.Data.Repositories;
using Version = DeviceHive.Core.Version;

namespace DeviceHive.API.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class HomeController : BaseController
    {
        private ITimestampRepository _timestampRepository;

        public HomeController(ITimestampRepository timestampRepository)
        {
            _timestampRepository = timestampRepository;
        }

        public HttpResponseMessage Get()
        {
            var timestamp = _timestampRepository.GetCurrentTimestamp();
            return new HttpResponseMessage
            {
                Content = new StringContent("The DeviceHive RESTful API is now running, " +
                    "please refer to documentation to get the list of available resources." +
                    "\n\nApi Version: " + Version.ApiVersion +
                    "\nServer Timestamp: " + timestamp.ToString("yyyy-MM-ddTHH:mm:ss.ffffff")),
            };
        }
    }
}
