using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using DeviceHive.Data.Model;

namespace DeviceHive.API
{
    public class CallContext
    {
        public User CurrentUser { get; set; }
        public AccessKey CurrentAccessKey { get; set; }
        public Device CurrentDevice { get; set; }

        public List<UserNetwork> CurrentUserNetworks { get; set; }
        public AccessKeyPermission[] CurrentUserPermissions { get; set; }
    }
}