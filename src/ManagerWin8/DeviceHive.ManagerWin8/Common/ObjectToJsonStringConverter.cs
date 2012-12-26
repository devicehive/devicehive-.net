using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace DeviceHive.ManagerWin8.Common
{
    class ObjectToJsonStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null)
            {
                return "";
            }
            using (var stringWriter = new StringWriter())
            {
                var serializer = new JsonSerializer();
                serializer.NullValueHandling = NullValueHandling.Ignore;
                serializer.Serialize(stringWriter, value);
                return stringWriter.ToString();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is string && (string)value == "")
            {
                return null;
            }
            using (var reader = new StringReader((string)value))
            {
                var serializer = new JsonSerializer();
                serializer.NullValueHandling = NullValueHandling.Ignore;
                return serializer.Deserialize(reader, typeof(JToken));
            }
        }

    }
}
