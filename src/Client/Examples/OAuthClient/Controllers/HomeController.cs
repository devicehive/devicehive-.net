using DeviceHive.Client;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace OAuthClient.Controllers
{
    // Before running this example application, please make sure you have added an OAuth Client to the DeviceHive.
    // The OAuth Client entity must have the following properties set:
    //     - domain: should correspond to the domain of the current application
    //     - redirectUrl: should correspond to the redurect URL of the current page (http://<domain>/Home/Exchange)
    //     - oauthId: an arbitrary value
    // Also make sure the controller properties below are set in accordance to the OAuth Client entity and DeviceHive configuration.

    public class HomeController : Controller
    {
        private const string OAuthUrl = "http://localhost/DeviceHive.Admin/oauth2";                       // DeviceHive OAuth authorization URL
        private const string OAuthTokenUrl = "http://localhost/DeviceHive.API/oauth2/token";              // DeviceHive OAuth token URL
        private const string OAuthRedirectUrl = "http://localhost/DeviceHive.OAuthClient/Home/Exchange";  // OAuth redirect URL in the current application
        private const string DeviceHiveUrl = "http://localhost/DeviceHive.API";                           // URL of the DeviceHive API
        private const string ClientID = "Examples.OAuthClient";                                           // OAuth name of the current application
        private const string ClientSecret = "";                                                           // OAuth secret of the current application
        private const string OAuthScope = "GetDevice";                                                    // Requested OAuth scope

        private string AccessKey
        {
            get { return (string)Session["AccessKey"]; }
            set { Session["AccessKey"] = value; }
        }

        public ActionResult Index()
        {
            // if DeviceHive access key is uavailable - offer OAuth authentication
            if (AccessKey == null)
                return View();

            // otherwise, read and display a list of DeviceHive devices
            try
            {
                var service = new RestfulClientService(DeviceHiveUrl, AccessKey, true);
                var devices = service.GetDevices();
                return View("Devices", devices);
            }
            catch (ClientServiceException ex)
            {
                var webException = (System.Net.WebException)ex.InnerException;
                var response = (System.Net.HttpWebResponse)webException.Response;
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    return View();

                throw;
            }
        }

        [HttpPost]
        public ActionResult RequestAccess()
        {
            // redirect the user to the DeviceHive OAuth endpoint
            var url = string.Format("{0}?response_type=code&client_id={1}&scope={2}&redirect_uri={3}", OAuthUrl, ClientID, "GetDevice", OAuthRedirectUrl);
            return Redirect(url);
        }

        [HttpPost]
        public ActionResult ClearAccess()
        {
            // clear access key and redirect to the Index page
            AccessKey = null;
            return RedirectToAction("Index");
        }

        public async Task<ActionResult> Exchange(string code, string error, string state)
        {
            // the action is invoked by OAuth authorization server

            if (error != null)
                return Content("The OAuthServer returned an error: " + error);

            if (string.IsNullOrEmpty(code))
                return Content("The OAuthServer did not return authorization code!");

            try
            {
                // exchange authorization code to DeviceHive access key
                AccessKey = await ExchangeAuthCode(code);
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                return Content("Error: " + ex.Message);
            }
        }

        private async Task<string> ExchangeAuthCode(string code)
        {
            using (var httpClient = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Post, OAuthTokenUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(Encoding.UTF8.GetBytes(string.Format("{0}:{1}", ClientID, ClientSecret))));
                request.Content = new FormUrlEncodedContent(new Dictionary<string, string> {
                    { "code", code },
                    { "redirect_uri", OAuthRedirectUrl },
                    { "grant_type", "authorization_code" },
                });

                var response = await httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                    throw new Exception("The OAuthServer returned a HTTP error: " + response.StatusCode);

                var responseJson = JObject.Parse(await response.Content.ReadAsStringAsync());
                if (responseJson["access_token"] == null)
                    throw new Exception("The OAuthServer did not return access token!");

                return (string)responseJson["access_token"];
            }
        }
    }
}
