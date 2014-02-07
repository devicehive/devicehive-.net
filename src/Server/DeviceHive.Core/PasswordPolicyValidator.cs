using System;
using System.Text.RegularExpressions;

namespace DeviceHive.Core
{
    /// <summary>
    /// Provides user password policy validation.
    /// </summary>
    public class PasswordPolicyValidator
    {
        private DeviceHiveConfiguration _configuration;

        /// <summary>
        /// Default constuctor
        /// </summary>
        /// <param name="configuration">DeviceHiveConfiguration object</param>
        public PasswordPolicyValidator(DeviceHiveConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException("configuration");

            _configuration = configuration;
        }

        /// <summary>
        /// Validate password to ensure it matches user password policy.
        /// </summary>
        /// <param name="password">The password to validate.</param>
        public void Validate(string password)
        {
            if (password == null)
                throw new ArgumentNullException("password");

            // get password policy configuration
            var policy = _configuration.UserPasswordPolicy;

            // validate complexity
            switch (policy.ComplexityLevel)
            {
                case 0:
                    break;
                case 1:
                    if (!Regex.IsMatch(password, @"(?=.*[\d\W])(?=.*[a-zA-Z])"))
                        throw new PasswordPolicyViolationException("The password must contain both letters and numbers!");
                    break;
                case 2:
                    if (!Regex.IsMatch(password, @"(?=.*[\d\W])(?=.*[a-z])(?=.*[A-Z])"))
                        throw new PasswordPolicyViolationException("The password must contain lower and upper letters and numbers!");
                    break;
                case 3:
                    if (!Regex.IsMatch(password, @"(?=.*\d)(?=.*\W)(?=.*[a-z])(?=.*[A-Z])"))
                        throw new PasswordPolicyViolationException("The password must contain lower and upper letters, numbers and special characters!");
                    break;
                default:
                    throw new InvalidOperationException(
                        "The configuration declares invalid complexity level for user password policy!" +
                        " Permitted values are between 0 (no restriction) and 3 (most restrictive)");
            }

            // validate length
            if (password.Length < policy.MinLength)
                throw new PasswordPolicyViolationException(string.Format("The password is too short! The minimum length is {0}.", policy.MinLength));
        }
    }

    /// <summary>
    /// Represents password policy violation exception
    /// </summary>
    public class PasswordPolicyViolationException : Exception
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="message">Exception message to be presented to the client.</param>
        public PasswordPolicyViolationException(string message) :
            base(message)
        {
        }
    }
}
