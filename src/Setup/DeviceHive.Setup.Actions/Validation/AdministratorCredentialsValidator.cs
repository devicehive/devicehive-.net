using System;
using System.Text.RegularExpressions;

namespace DeviceHive.Setup.Actions
{
    class AdministratorCredentialsValidator
    {
        private const int PASSWORD_MIN_LENGTH = 8;

        public void Validate(string login, string password)
        {
            if (string.IsNullOrEmpty(login))
                throw new Exception("The login is empty. Please enter a correct value.");

            if (string.IsNullOrEmpty(password))
                throw new Exception("The password is empty. Please enter a correct value.");

            if (password.Length < PASSWORD_MIN_LENGTH)
                throw new Exception("The password is too short! The minimum length is 8.");

            if (!Regex.IsMatch(password, @"(?=.*[\d\W])(?=.*[a-z])(?=.*[A-Z])"))
                throw new Exception("The password must contain lower and upper letters and numbers!");
        }
    }
}