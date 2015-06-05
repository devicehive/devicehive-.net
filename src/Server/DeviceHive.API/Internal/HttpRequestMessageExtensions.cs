using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Web;

namespace DeviceHive.API.Internal
{
    public static class HttpRequestMessageExtensions
    {
        private static string _userAddressHeader = ConfigurationManager.AppSettings["UserAddressHeader"];

        public static string GetUserAddress(this HttpRequestMessage request)
        {
            // try user address header
            if (!string.IsNullOrEmpty(_userAddressHeader))
            {
                var userAddress = request.GetCustomHeader(_userAddressHeader);
                if (!string.IsNullOrEmpty(userAddress))
                {
                    // use the last address as most trustworthy
                    return userAddress.Split(',').Last().Trim();
                }
            }

            // try HttpRequestBase property
            if (request.Properties.ContainsKey("MS_HttpContext"))
            {
                return ((HttpContextWrapper)request.Properties["MS_HttpContext"]).Request.UserHostAddress;
            }

            throw new Exception("Unable to determine client IP address!");
        }

        public static string GetCustomHeader(this HttpRequestMessage request, string name)
        {
            IEnumerable<string> values;
            if (!request.Headers.TryGetValues(name, out values))
                return null;

            return values.First();
        }
    }
}