using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceHive.Client
{
    internal class RestClient
    {
        private HttpClient _httpClient;
        private JsonMediaTypeFormatter _jsonFormatter;
        private DeviceHiveConnectionInfo _connectionInfo;

        #region Constructor

        public RestClient(DeviceHiveConnectionInfo connectionInfo)
        {
            if (connectionInfo == null)
                throw new ArgumentNullException("connectionInfo");

            _connectionInfo = connectionInfo;

            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(_connectionInfo.ServiceUrl.TrimEnd('/') + "/");
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            if (_connectionInfo.AccessKey != null)
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", connectionInfo.AccessKey);
            }
            else if (_connectionInfo.Login != null)
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(
                    Encoding.UTF8.GetBytes(string.Format("{0}:{1}", _connectionInfo.Login, _connectionInfo.Password))));
            }

            _jsonFormatter = new JsonMediaTypeFormatter();
            _jsonFormatter.SerializerSettings.Converters.Add(new IsoDateTimeConverter { DateTimeFormat = "yyyy-MM-ddTHH:mm:ss.ffffff" });
            _jsonFormatter.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
            _jsonFormatter.SerializerSettings.ContractResolver = new JsonContractResolver();
        }
        #endregion

        #region Public Methods

        public async Task<T> GetAsync<T>(string url)
        {
            return await GetAsync<T>(url, CancellationToken.None);
        }

        public async Task<T> GetAsync<T>(string url, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _httpClient.GetAsync(url, cancellationToken);
                ValidateResponseStatus(response);

                return await response.Content.ReadAsAsync<T>(new[] { _jsonFormatter });
            }
            catch (DeviceHiveException)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new DeviceHiveException("Network error while sending request to the DeviceHive server", ex);
            }
        }

        public async Task<T> PostAsync<T>(string url, T value)
        {
            try
            {
                var response = await _httpClient.PostAsync(url, value, _jsonFormatter);
                ValidateResponseStatus(response);

                return await response.Content.ReadAsAsync<T>(new[] { _jsonFormatter });
            }
            catch (DeviceHiveException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new DeviceHiveException("Network error while sending request to the server", ex);
            }
        }

        public async Task PutAsync<T>(string url, T value)
        {
            try
            {
                var response = await _httpClient.PutAsync(url, value, _jsonFormatter);
                ValidateResponseStatus(response);
            }
            catch (DeviceHiveException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new DeviceHiveException("Network error while sending request to the server", ex);
            }
        }

        public async Task DeleteAsync(string url)
        {
            try
            {
                var response = await _httpClient.DeleteAsync(url);
                ValidateResponseStatus(response);
            }
            catch (DeviceHiveException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new DeviceHiveException("Network error while sending request to the server", ex);
            }
        }

        public string MakeQueryString<T>(T query)
        {
            if (query == null)
                return null;

            var serializer = new JsonSerializer();
            serializer.NullValueHandling = NullValueHandling.Ignore;
            serializer.DefaultValueHandling = DefaultValueHandling.Ignore;
            serializer.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializer.Converters.Add(new StringEnumConverter());
            serializer.Converters.Add(new IsoDateTimeConverter { DateTimeFormat = "yyyy-MM-ddTHH:mm:ss.ffffff" });

            var jObject = JObject.FromObject(query, serializer);
            if (!jObject.Properties().Any())
                return null;

            return "?" + string.Join("&", jObject.Properties().Select(p => p.Name + "=" + Uri.EscapeDataString(p.Value.ToString())));
        }
        #endregion

        #region Private Methods

        private HttpResponseMessage ValidateResponseStatus(HttpResponseMessage response)
        {
            if (response.StatusCode == HttpStatusCode.Unauthorized)
                throw new DeviceHiveUnauthorizedException("Supplied credentials are invalid or access is denied!");

            if (response.StatusCode == HttpStatusCode.BadRequest ||
                response.StatusCode == HttpStatusCode.Forbidden ||
                response.StatusCode == HttpStatusCode.NotFound ||
                response.StatusCode == HttpStatusCode.MethodNotAllowed)
            {
                var errorDetail = ReadErrorDetail(response);
                var message = errorDetail != null && !string.IsNullOrEmpty(errorDetail.Message) ?
                    "DeviceHive server returns an error: " + errorDetail.Message :
                    "DeviceHive server returns an unspecified error!";
                throw new DeviceHiveException(message);
            }

            if (!response.IsSuccessStatusCode)
                throw new DeviceHiveException("DeviceHive server returned an internal server error!");

            return response;
        }

        private ErrorDetail ReadErrorDetail(HttpResponseMessage response)
        {
            try
            {
                return response.Content.ReadAsAsync<ErrorDetail>(new[] { _jsonFormatter }).Result;
            }
            catch (Exception)
            {
                return null;
            }
        }
        #endregion

        #region ErrorDetail class

        private class ErrorDetail
        {
            public int? Error { get; set; }
            public string Message { get; set; }
        }
        #endregion
    }
}
