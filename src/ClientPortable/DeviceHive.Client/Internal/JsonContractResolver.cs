using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Reflection;

namespace DeviceHive.Client
{
    internal class JsonContractResolver : CamelCasePropertyNamesContractResolver
    {
        #region DefaultContractResolver Members

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);
            if (property.DeclaringType == typeof(Notification) && property.PropertyName == "name")
            {
                property.PropertyName = "notification";
            }
            if (property.DeclaringType == typeof(Command) && property.PropertyName == "name")
            {
                property.PropertyName = "command";
            }
            return property;
        }
        #endregion
    }
}
