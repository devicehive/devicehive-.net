using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using DeviceHive.Data.Model;

namespace DeviceHive.API
{
    public class RequestContext
    {
        public User CurrentUser { get; set; }
        public Device CurrentDevice { get; set; }

        public List<UserNetwork> CurrentUserNetworks { get; set; }
    }
}