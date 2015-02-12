using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceHive.Client
{
    /// <summary>
    /// Represents a default implementation of the <see cref="IRestClient"/> which uses HttpClient for making HTTP requests.
    /// </summary>
    public class RestClient : IRestClient
    {
        private HttpClient _httpClient;
        private JsonSerializerSettings _jsonSettings;

        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="connectionInfo">An instance of <see cref="DeviceHiveConnectionInfo" /> class which provides DeviceHive connection information.</param>
        public RestClient(DeviceHiveConnectionInfo connectionInfo)
            : this(connectionInfo, null)
        {
        }

        /// <summary>
        /// Constructor which allows to set custom <see cref="HttpMessageHandler" /> for handling HTTP requests.
        /// </summary>
        /// <param name="connectionInfo">An instance of <see cref="DeviceHiveConnectionInfo" /> class which provides DeviceHive connection information.</param>
        /// <param name="httpMessageHandler">An instance of <see cref="HttpMessageHandler"/> class.</param>
        public RestClient(DeviceHiveConnectionInfo connectionInfo, HttpMessageHandler httpMessageHandler)
        {
            if (connectionInfo == null)
                throw new ArgumentNullException("connectionInfo");

            _httpClient = httpMessageHandler != null ? new HttpClient(httpMessageHandler) : new HttpClient();
            _httpClient.BaseAddress = new Uri(connectionInfo.ServiceUrl.TrimEnd('/') + "/");
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            if (connectionInfo.AccessKey != null)
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", connectionInfo.AccessKey);
            }
            else if (connectionInfo.Login != null)
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(
                    Encoding.UTF8.GetBytes(string.Format("{0}:{1}", connectionInfo.Login, connectionInfo.Password))));
            }

            _jsonSettings = new JsonSerializerSettings();
            _jsonSettings.Converters.Add(new IsoDateTimeConverter { DateTimeFormat = "yyyy-MM-ddTHH:mm:ss.ffffff" });
            _jsonSettings.NullValueHandling = NullValueHandling.Ignore;
            _jsonSettings.ContractResolver = new JsonContractResolver();
        }
        #endregion

        #region IRestClient Members

        /// <summary>
        /// Invokes a HTTP method on the DeviceHive server.
        /// </summary>
        /// <typeparam name="T">Resource type.</typeparam>
        /// <param name="method">HTTP method.</param>
        /// <param name="url">Relative URL to the resource.</param>
        /// <param name="content">Resource content to send (for POST and PUT methods).</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A resource returned from the server (for GET and POST methods).</returns>
        public async Task<T> InvokeAsync<T>(HttpMethod method, string url, T content = null, CancellationToken? cancellationToken = null)
            where T : class
        {
            var request = new HttpRequestMessage(method, url);
            if (method == HttpMethod.Post || method == HttpMethod.Put)
            {
                if (content == null)
                    throw new InvalidOperationException("Content is required for POST and PUT methods!");

                request.Content = CreateJsonContent(content);
            }

            try
            {
                var response = await _httpClient.SendAsync(request, cancellationToken ?? CancellationToken.None);
                await ValidateResponseStatusAsync(response);

                if (method == HttpMethod.Get || method == HttpMethod.Post)
                    return await ReadAsAsync<T>(response.Content);

                return null;
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
        #endregion

        #region Internal Static Methods

        /// <summary>
        /// Creates a query string by serializing passed object.
        /// </summary>
        /// <typeparam name="T">Type of the object.</typeparam>
        /// <param name="query">Query object.</param>
        /// <returns>A query string created by serializing the object.</returns>
        internal static string MakeQueryString<T>(T query)
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

        private HttpContent CreateJsonContent<T>(T value)
        {
            var json = JsonConvert.SerializeObject(value, _jsonSettings);
            return new StringContent(json, Encoding.UTF8, "application/json");
        }

        private async Task<HttpResponseMessage> ValidateResponseStatusAsync(HttpResponseMessage response)
        {
            if (response.StatusCode == HttpStatusCode.Unauthorized)
                throw new DeviceHiveUnauthorizedException("Supplied credentials are invalid or access is denied!");

            if (response.StatusCode == HttpStatusCode.BadRequest ||
                response.StatusCode == HttpStatusCode.Forbidden ||
                response.StatusCode == HttpStatusCode.NotFound ||
                response.StatusCode == HttpStatusCode.MethodNotAllowed)
            {
                var errorDetail = await ReadErrorDetailAsync(response.Content);
                var message = errorDetail != null && !string.IsNullOrEmpty(errorDetail.Message) ?
                    "DeviceHive server returns an error: " + errorDetail.Message :
                    "DeviceHive server returns an unspecified error!";
                throw new DeviceHiveException(message);
            }

            if (!response.IsSuccessStatusCode)
                throw new DeviceHiveException("DeviceHive server returned an internal server error!");

            return response;
        }

        private async Task<T> ReadAsAsync<T>(HttpContent content)
        {
            if (content.Headers.ContentType == null)
                throw new DeviceHiveException("DeviceHive server did not return Content-Type header!");

            if (!string.Equals(content.Headers.ContentType.MediaType, "application/json", StringComparison.OrdinalIgnoreCase))
                throw new DeviceHiveException("DeviceHive server returned content with unexpected content type: " + content.Headers.ContentType.MediaType);

            var json = await content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(json, _jsonSettings);
        }

        private async Task<ErrorDetail> ReadErrorDetailAsync(HttpContent content)
        {
            try
            {
                return await ReadAsAsync<ErrorDetail>(content);
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
