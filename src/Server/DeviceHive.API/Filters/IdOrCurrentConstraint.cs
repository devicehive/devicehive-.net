using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http.Routing;

namespace DeviceHive.API.Filters
{
    public class IdOrCurrentConstraint : IHttpRouteConstraint
    {
        public bool Match(HttpRequestMessage request, IHttpRoute route, string parameterName,
            IDictionary<string, object> values, HttpRouteDirection routeDirection)
        {
            object value;
            if (!values.TryGetValue(parameterName, out value) || value == null)
                return false;

            var valueString = Convert.ToString(value);
            if (string.Equals(valueString, "current", StringComparison.OrdinalIgnoreCase))
                return true;

            int intValue;
            return int.TryParse(valueString, out intValue);
        }
    }
}