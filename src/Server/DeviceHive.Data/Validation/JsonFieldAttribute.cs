using System;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace DeviceHive.Data.Validation
{
    public class JsonFieldAttribute : ValidationAttribute
    {
        #region ValidationAttribute Members

        public override bool IsValid(object value)
        {
            if (value == null)
                return true;

            var strValue = value.ToString();
            if (string.IsNullOrWhiteSpace(strValue))
                return false;

            try
            {
                JsonConvert.DeserializeObject(strValue);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public override string FormatErrorMessage(string name)
        {
            return string.Format("The {0} field is not a valid JSON value!", name);
        }
        #endregion
    }
}