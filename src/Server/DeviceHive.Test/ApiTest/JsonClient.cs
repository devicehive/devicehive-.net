using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DeviceHive.Test
{
    public class JsonClient
    {
        public string BaseUrl { get; private set; }

        public JsonClient(string baseUrl)
        {
            BaseUrl = baseUrl;
        }

        public JsonResponse Get(string url, Authorization auth = null)
        {
            return Run("GET", url, auth: auth);
        }

        public JsonResponse Post(string url, object request, Authorization auth = null)
        {
            return Run("POST", url, jsonRequest: JObject.FromObject(request), auth: auth);
        }

        public JsonResponse Put(string url, object request, Authorization auth = null)
        {
            return Run("PUT", url, jsonRequest: JObject.FromObject(request), auth: auth);
        }

        public JsonResponse Delete(string url, Authorization auth = null)
        {
            return Run("DELETE", url, auth: auth);
        }

        public JsonResponse Run(string method, string url, JObject jsonRequest = null, Authorization auth = null)
        {
            if (string.IsNullOrEmpty(method))
                throw new ArgumentException("Method is null or empty!", "method");
            if (string.IsNullOrEmpty(url))
                throw new ArgumentException("URL is null or empty!", "url");

            // prepare request
            var request = (HttpWebRequest)HttpWebRequest.Create(BaseUrl + url);
            request.Headers.Add("ClientVersion", DeviceHive.Core.Version.ApiVersion);
            request.Method = method;
            request.Accept = "application/json";
            if (auth != null)
            {
                switch (auth.Type)
                {
                    case "User":
                        request.Headers["Authorization"] = "Basic " + Convert.ToBase64String(
                            Encoding.UTF8.GetBytes(string.Format("{0}:{1}", auth.Login, auth.Password)));
                        break;
                    case "AccessKey":
                        request.Headers["Authorization"] = "Bearer " + auth.Login;
                        break;
                    case "Device":
                        request.Headers["Auth-DeviceID"] = auth.Login;
                        request.Headers["Auth-DeviceKey"] = auth.Password;
                        break;
                }
            }
            if (jsonRequest != null)
            {
                request.ContentType = "application/json";
                using (var stream = request.GetRequestStream())
                {
                    using (var writer = new StreamWriter(stream))
                    {
                        writer.Write(jsonRequest.ToString());
                    }
                }
            }

            // perform a call
            HttpWebResponse response;
            try
            {
                response = (HttpWebResponse)request.GetResponse();
            }
            catch (WebException ex)
            {
                response = (HttpWebResponse)ex.Response;
            }

            // parse response
            using (var stream = response.GetResponseStream())
            {
                using (var reader = new StreamReader(stream))
                {
                    var responseString = reader.ReadToEnd();
                    try
                    {
                        var json = string.IsNullOrEmpty(responseString) ? null : JToken.Parse(responseString);
                        return new JsonResponse((int)response.StatusCode, json);
                    }
                    catch (JsonReaderException ex)
                    {
                        throw new WebException(string.Format("Error while parsing server response! " +
                            "Status: {0}, Response: {1}", (int)response.StatusCode, responseString), ex);
                    }
                }
            }
        }
    }

    public class Authorization
    {
        public string Type { get; private set; }
        public string Login { get; private set; }
        public string Password { get; private set; }
        public int? ID { get; private set; }

        public Authorization(string type, string login, string password = null, int? id = null)
        {
            Type = type;
            Login = login;
            Password = password;
            ID = id;
        }
    }

    public class JsonResponse
    {
        public int Status { get; private set; }
        public JToken Json { get; private set; }

        public JsonResponse(int status, JToken json)
        {
            Status = status;
            Json = json;
        }
    }
}
