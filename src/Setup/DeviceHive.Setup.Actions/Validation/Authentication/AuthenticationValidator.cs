using System;

namespace DeviceHive.Setup.Actions
{
    public abstract class AuthenticationValidator
    {
        protected string ProviderName { get; set; }

        public void Validate(string clientId, string clientSecret)
        {
            if (string.IsNullOrEmpty(clientId))
            {
                throw new Exception(string.Format("Client Id for Authentication {0} provider is empty. Please enter a correct value.", ProviderName));
            }

            if (string.IsNullOrEmpty(clientSecret))
            {
                throw new Exception(string.Format("Client Secret for Authentication {0} provider is empty. Please enter a correct value.", ProviderName));
            }
        }
    }
}
