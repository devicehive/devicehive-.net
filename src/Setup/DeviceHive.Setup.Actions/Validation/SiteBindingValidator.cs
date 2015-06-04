using System;
using System.Linq;
using Microsoft.Web.Administration;
using System.Globalization;
using System.Text.RegularExpressions;

namespace DeviceHive.Setup.Actions.Validation
{
    public class SiteBindingValidator
    {
        public void Validate(string webSiteName, string hostName, string portNumber)
        {
            if (string.IsNullOrEmpty(webSiteName))
                throw new ArgumentNullException("webSiteName");

            ValidateHostName(hostName);
            ValidatePortNumber(portNumber);

            using (var serverManager = new ServerManager())
            {
                int portNumberValue = Convert.ToInt32(portNumber);

                foreach (var site in serverManager.Sites.Where(s => !string.Equals(s.Name, webSiteName, StringComparison.Ordinal) && s.State == ObjectState.Started))
                {
                    foreach (var binding in site.Bindings)
                    {
                        if (binding.EndPoint.Port != portNumberValue)
                            continue;

                        if (string.IsNullOrEmpty(hostName) && string.IsNullOrEmpty(binding.Host))
                        {
                            throw new Exception(string.Format("The specified port number '{0}' already used by '{1}' site.", binding.EndPoint.Port, site.Name));
                        }
                        else if (string.Equals(binding.Host, hostName, StringComparison.Ordinal))
                        {
                            throw new Exception(string.Format("The specified host name '{0}' and port Number '{1}' already used by '{2}' site.", binding.Host, binding.EndPoint.Port, site.Name));
                        }
                    }
                }
            }
        }

        private void ValidateHostName(string hostName)
        {
            if (!string.IsNullOrEmpty(hostName) && !string.Equals(hostName, "*", StringComparison.Ordinal))
            {
                Regex numeric = new Regex(@"^((?!-)[A-Za-z\d-]{1,63}(?<!-)\.?)+[A-Za-z]{2,6}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                if (!numeric.IsMatch(hostName))
                    throw new Exception("The specified host name is incorrect. The host name must use a valid host name format and cannot contain the following characters: \"/\\[]:|<>+=;,?*$%#@{}()^`_.");
            }
        }

        private void ValidatePortNumber(string portNumber)
        {
            if (string.IsNullOrEmpty(portNumber))
                throw new Exception("The port number is empty. Please enter a correct value.");

            Regex numeric = new Regex(@"^[1-9]\d{1,5}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            if (!numeric.IsMatch(portNumber))
                throw new Exception("The specified port number is invalid. Please enter a correct value.");

            int value = Convert.ToInt32(portNumber);
            if (value < 1 || value > 65535)
                throw new Exception("The specified port number is out of range. Please enter a correct value.");
        }
    }
}
