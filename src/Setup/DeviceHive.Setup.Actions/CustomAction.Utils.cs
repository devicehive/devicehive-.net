
using System;
using Microsoft.Deployment.WindowsInstaller;

namespace DeviceHive.Setup.Actions
{
    public partial class CustomActions
    {
        private static bool GetPropertyBoolValue(Session session, string propertyName, bool required = false)
        {
            string propertyValue = session[propertyName];
            if (string.IsNullOrEmpty(propertyValue))
            {
                string errorMessage = string.Format("{0} property has not been set up.", propertyName);
                session.Log(errorMessage);
                if (required)
                {
                    throw new ArgumentException(errorMessage);
                }
                return false;
            }

            bool result;
            if (!bool.TryParse(propertyValue, out result))
            {
                throw new ArgumentException(string.Format("{0} property has wrong type value.", propertyName));
            }
            return result;
        }

        public static string GetPropertyStringValue(Session session, string propertyName, bool required = false)
        {
            string propertyValue = session[propertyName];
            if (string.IsNullOrEmpty(propertyValue))
            {
                string errorMessage = string.Format("{0} property has not been set up.", propertyName);
                session.Log(errorMessage);
                if (required)
                {
                    throw new ArgumentException(errorMessage);
                }
                return string.Empty;
            }
            return propertyValue.Trim();
        }

        private static void CreateComboBoxRecordItem(View view, int numRows, string propertyName, string text, string value)
        {
            Record record = new Record(4);
            record.SetString(1, propertyName);
            record.SetInteger(2, numRows);
            record.SetString(3, value);
            record.SetString(4, text);
            view.Modify(ViewModifyMode.InsertTemporary, record);
        }
    }
}
