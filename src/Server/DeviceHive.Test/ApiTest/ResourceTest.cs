using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using NUnit.Framework.Constraints;

namespace DeviceHive.Test
{
    /// <summary>
    /// Represents base class for resource-based API tests
    /// </summary>
    public abstract class ResourceTest : AssertionHelper
    {
        private Stack<string> _resources;

        #region Protected Properties

        /// <summary>
        /// Gets resource relative URI
        /// </summary>
        protected string ResourceUri { get; set; }

        /// <summary>
        /// Gets JsonClient object
        /// </summary>
        protected internal JsonClient Client { get; private set; }

        /// <summary>
        /// Gets or sets expected status code for create POST call
        /// </summary>
        protected int ExpectedCreatedStatus { get; set; }

        /// <summary>
        /// Gets or sets expected status code for PUT call
        /// </summary>
        protected int ExpectedUpdatedStatus { get; set; }

        /// <summary>
        /// Gets or sets expected status code for DELETE call
        /// </summary>
        protected int ExpectedDeletedStatus { get; set; }

        /// <summary>
        /// Gets or sets unexisting resource ID to test for 404 calls
        /// </summary>
        protected int UnexistingResourceID { get; set; }

        /// <summary>
        /// Gets or sets username of existing administrator account
        /// </summary>
        protected string ExistingAdminUsername { get; set; }

        /// <summary>
        /// Gets or sets password of existing administrator account
        /// </summary>
        protected string ExistingAdminPassword { get; set; }

        /// <summary>
        /// Gets or sets default password set for new users
        /// </summary>
        protected string NewUserPassword { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="resourceUri">Resource relative URI</param>
        public ResourceTest(string resourceUri)
        {
            if (string.IsNullOrEmpty(resourceUri))
                throw new ArgumentException("ResourceUri is null or empty!", "resourceUri");

            ResourceUri = resourceUri;
            Client = new JsonClient(ConfigurationManager.AppSettings["ApiUrl"]);

            ExpectedCreatedStatus = 201;
            ExpectedUpdatedStatus = 204;
            ExpectedDeletedStatus = 204;
            UnexistingResourceID = 999999;
            ExistingAdminUsername = "dhadmin";
            ExistingAdminPassword = "dhadmin_#911";
            NewUserPassword = "Qwe12345!";
        }
        #endregion

        #region Protected Methods

        /// <summary>
        /// Test set up method
        /// </summary>
        [SetUp]
        protected virtual void SetUp()
        {
            _resources = new Stack<string>();
            OnCreateDependencies();
        }

        /// <summary>
        /// Test tear down method
        /// Default implementation deletes all created resources
        /// </summary>
        [TearDown]
        protected virtual void TearDown()
        {
            foreach (var resource in _resources)
            {
                Client.Delete(resource, auth: Admin);
            }
            _resources = null;
        }

        /// <summary>
        /// Creates dependencies required to test current resource
        /// </summary>
        protected virtual void OnCreateDependencies()
        {
        }

        /// <summary>
        /// Gets list of resources from the server
        /// </summary>
        /// <param name="auth">Authorization info</param>
        /// <returns>JArray that represents server response</returns>
        protected virtual JArray List(Authorization auth = null)
        {
            // invoke get
            var response = Client.Get(ResourceUri, auth: auth);

            // verify response object
            ExpectResponseStatus(response, 200);
            Expect(response.Json, Is.InstanceOf<JArray>());
            return (JArray)response.Json;
        }

        /// <summary>
        /// Gets list of resources from the server
        /// </summary>
        /// <param name="query">Optional query parameters</param>
        /// <param name="auth">Authorization info</param>
        /// <returns>JArray that represents server response</returns>
        protected virtual JArray List(Dictionary<string, string> query, Authorization auth = null)
        {
            // invoke get
            var response = Client.Get(ResourceUri + "?" +
                string.Join("&", query.Select(q => string.Format("{0}={1}", q.Key, Uri.EscapeUriString(q.Value)))), auth: auth);

            // verify response object
            ExpectResponseStatus(response, 200);
            Expect(response.Json, Is.InstanceOf<JArray>());
            return (JArray)response.Json;
        }

        /// <summary>
        /// Gets resource from the server
        /// </summary>
        /// <param name="resource">Resource to get</param>
        /// <param name="auth">Authorization info</param>
        /// <returns>JObject that represents server response</returns>
        protected virtual JObject Get(object resource, Authorization auth = null)
        {
            // invoke get
            var resourceId = GetResourceId(resource);
            var response = Client.Get(ResourceUri + "/" + resourceId, auth: auth);

            // verify response object
            ExpectResponseStatus(response, 200);
            Expect(response.Json, Is.InstanceOf<JObject>());
            Expect(GetResourceId((JObject)response.Json), Is.EqualTo(resourceId.ToString()));
            return (JObject)response.Json;
        }

        /// <summary>
        /// Creates resource on the server
        /// </summary>
        /// <param name="resourceObject">Resource object to create</param>
        /// <param name="auth">Authorization info</param>
        /// <returns>JObject that represents server response</returns>
        protected virtual JObject Create(object resourceObject, Authorization auth = null)
        {
            // invoke create
            var response = Client.Post(ResourceUri, resourceObject, auth: auth);

            // verify response object
            ExpectResponseStatus(response, ExpectedCreatedStatus);
            Expect(response.Json, Is.InstanceOf<JObject>());
            RegisterForDeletion(ResourceUri + "/" + GetResourceId((JObject)response.Json));
            return (JObject)response.Json;
        }


        /// <summary>
        /// Updates resource on the server
        /// </summary>
        /// <param name="resource">Resource to update</param>
        /// <param name="resourceObject">Resource object to send as update</param>
        /// <param name="auth">Authorization info</param>
        protected virtual void Update(object resource, object resourceObject, Authorization auth = null)
        {
            // invoke update
            var resourceId = GetResourceId(resource);
            var response = Client.Put(ResourceUri + "/" + resourceId, resourceObject, auth: auth);

            // verify response object
            ExpectResponseStatus(response, ExpectedUpdatedStatus);
            //Expect(response.Json, Is.InstanceOf<JObject>());
            //Expect(GetResourceId((JObject)response.Json), Is.EqualTo(resourceId.ToString()));
            //return (JObject)response.Json;
        }

        /// <summary>
        /// Deletes resource on the server
        /// </summary>
        /// <param name="resource">Resource to delete</param>
        /// <param name="auth">Authorization info</param>
        protected virtual void Delete(object resource, Authorization auth = null)
        {
            // invoke delete
            var resourceId = GetResourceId(resource);
            var response = Client.Delete(ResourceUri + "/" + resourceId, auth: auth);

            // verify response object
            ExpectResponseStatus(response, ExpectedDeletedStatus);
        }

        /// <summary>
        /// Gets resource identifier
        /// </summary>
        /// <param name="resource">Resource identifier or JObject representing the resource</param>
        /// <returns>Resource identifier</returns>
        protected virtual string GetResourceId(object resource)
        {
            if (resource is JObject)
            {
                var id = (JValue)((JObject)resource)["id"];
                if (id == null)
                    throw new ArgumentException("Resource is JObject but does not include id property!", "resourceId");

                return id.Value.ToString();
            }
            return resource.ToString();
        }

        /// <summary>
        /// Creates a user and returns corresponding Authorization object
        /// </summary>
        /// <param name="role">User role</param>
        /// <param name="networks">List of network resources to associate user with</param>
        /// <returns>Coresponding Authorization object</returns>
        protected Authorization CreateUser(int role, params object[] networks)
        {
            // assign login
            var login = "_ut_" + Guid.NewGuid().ToString();

            // create user
            var userResource = Client.Post("/user", new { login = login, password = NewUserPassword, role = role, status = 0 }, auth: Admin);
            Expect(userResource.Status, Is.EqualTo(ExpectedCreatedStatus));
            var userId = GetResourceId(userResource.Json);
            RegisterForDeletion("/user/" + userId);

            // create user/network association
            foreach (var network in networks ?? new object[] { })
            {
                var networkId = GetResourceId(network);
                var userNetworkResponse = Client.Put("/user/" + userId + "/network/" + networkId, new { }, auth: Admin);
                Expect(userNetworkResponse.Status, Is.EqualTo(ExpectedUpdatedStatus));
                RegisterForDeletion("/user/" + userId + "/network/" + networkId);
            }

            // return user authorization object
            return User(login, NewUserPassword, userId);
        }

        /// <summary>
        /// Creates an access key and returns corresponding Authorization object
        /// </summary>
        /// <param name="user">User authorization object</param>
        /// <param name="action">Allowed access key action</param>
        /// <param name="networkIds">Allowed networks</param>
        /// <param name="deviceGuids">Allowed devices</param>
        /// <returns>Coresponding Authorization object</returns>
        protected Authorization CreateAccessKey(Authorization user, string action, int[] networkIds = null, string[] deviceGuids = null)
        {
            return CreateAccessKey(user, new[] { action }, networkIds, deviceGuids);
        }

        /// <summary>
        /// Creates an access key and returns corresponding Authorization object
        /// </summary>
        /// <param name="user">User authorization object</param>
        /// <param name="actions">Allowed access key actions</param>
        /// <param name="networkIds">Allowed networks</param>
        /// <param name="deviceGuids">Allowed devices</param>
        /// <returns>Coresponding Authorization object</returns>
        protected Authorization CreateAccessKey(Authorization user, string[] actions, int[] networkIds = null, string[] deviceGuids = null)
        {
            // create access key
            var accessKeyResource = Client.Post("/user/current/accesskey", new { label = "ut", permissions =
                new[] { new { actions = actions, networkIds = networkIds, deviceGuids = deviceGuids } } }, auth: user);
            Expect(accessKeyResource.Status, Is.EqualTo(ExpectedCreatedStatus));
            var accessKeyId = GetResourceId(accessKeyResource.Json);
            RegisterForDeletion("/user/" + (user.ID != null ? user.ID : "current") + "/accesskey/" + accessKeyId);

            return AccessKey((string)accessKeyResource.Json["key"]);
        }

        /// <summary>
        /// Registers resource for deletion when the test ends
        /// </summary>
        /// <param name="resource">Full resource url</param>
        protected void RegisterForDeletion(string resource)
        {
            _resources.Push(resource);
        }

        /// <summary>
        /// Gets default authorization for admin user
        /// </summary>
        protected Authorization Admin
        {
            get { return new Authorization("User", ExistingAdminUsername, ExistingAdminPassword); }
        }

        /// <summary>
        /// Gets authorization for a user
        /// </summary>
        /// <param name="login">User login</param>
        /// <param name="password">User password</param>
        /// <param name="id">User identifier (optional)</param>
        protected Authorization User(string login, string password, string id = null)
        {
            return new Authorization("User", login, password, id);
        }

        /// <summary>
        /// Gets authorization for an access key
        /// </summary>
        /// <param name="key">Access key</param>
        protected Authorization AccessKey(string key)
        {
            return new Authorization("AccessKey", key, null);
        }

        /// <summary>
        /// Gets authorization for a device
        /// </summary>
        /// <param name="id">Device identifier</param>
        /// <param name="key">Device key</param>
        protected Authorization Device(string id, string key)
        {
            return new Authorization("Device", id, key);
        }

        /// <summary>
        /// Gets FailsWith constraint
        /// </summary>
        /// <param name="status">Expected status</param>
        /// <returns>ResponseFailsWithContraint object</returns>
        protected ResponseFailsWithContraint FailsWith(int status)
        {
            return new ResponseFailsWithContraint(status);
        }

        /// <summary>
        /// Gets Matches constraint
        /// </summary>
        /// <param name="expected">Expected object</param>
        /// <returns>ResponseMatchesContraint object</returns>
        protected ResponseMatchesContraint Matches(object expected)
        {
            return new ResponseMatchesContraint(expected);
        }
        #endregion

        #region Private Methods

        private void ExpectResponseStatus(JsonResponse response, int expected)
        {
            if (response.Status != expected)
            {
                throw new ResourceException(response.Status,
                    string.Format("Invalid server response! Expected: {0}, Actual: {1}", expected, response.Status));
            }
        }
        #endregion
    }

    public class ResponseMatchesContraint : Constraint
    {
        private readonly Regex _timestampRegex = new Regex(@"\""\d{4}\-\d{2}\-\d{2}T\d{2}\:\d{2}\:\d{2}\.\d{1,6}\""");

        #region Public Properties

        public object Expected { get; private set; }

        public static object Timestamp
        {
            get { return new TimestampValue(); }
        }
        #endregion

        #region Constructor

        public ResponseMatchesContraint(object expected)
        {
            Expected = expected;
        }
        #endregion

        #region Constraint Members

        public override bool Matches(object actual)
        {
            this.actual = actual;

            var actualJson = (JToken)actual;
            if (actualJson == null)
                return false;

            return IsMatches(actualJson, Expected);
        }

        public override void WriteDescriptionTo(MessageWriter writer)
        {
            writer.Write(JToken.FromObject(Expected));
        }

        public override void WriteActualValueTo(MessageWriter writer)
        {
            writer.Write(actual);
        }
        #endregion

        #region Private Methods

        private bool IsMatches(JToken actual, object expected)
        {
            if (expected == null || (expected is string) || expected.GetType().IsValueType)
            {
                var jvalue = actual as JValue;
                if (jvalue == null)
                    return false;

                if (expected is TimestampValue)
                {
                    // a timestamp is expected
                    return jvalue.Value is DateTime && _timestampRegex.IsMatch(jvalue.Parent.ToString());
                }
                else
                {
                    if (expected is int)
                        expected = (long)(int)expected;

                    return object.Equals(jvalue.Value, expected);
                }
            }

            foreach (var property in expected.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var expectedProperty = property.GetValue(expected, null);
                var actualProperty = actual[property.Name];

                if (actualProperty is JValue)
                {
                    return IsMatches(actualProperty, expectedProperty);
                }
                else if (actualProperty is JObject)
                {
                    if (expectedProperty == null || expectedProperty.GetType().IsPrimitive)
                        return false;
                    if (!IsMatches(actualProperty, expectedProperty))
                        return false;
                }
                else if (actualProperty is JArray)
                {
                    if (expectedProperty == null || !(expectedProperty is IEnumerable))
                        return false;
                    foreach (var expectedItem in ((IEnumerable)expectedProperty))
                    {
                        if (!actualProperty.Any(ap => IsMatches(ap, expectedItem)))
                            return false;
                    }
                }
                else
                {
                    // unexpected type
                    return false;
                }
            }
            return true;
        }

        #endregion

        #region TimestampValue struct

        private struct TimestampValue
        {
        }
        #endregion
    }

    public class ResponseFailsWithContraint : Constraint
    {
        #region Public Properties

        public int Status { get; private set; }

        #endregion

        #region Constructor

        public ResponseFailsWithContraint(int status)
        {
            Status = status;
        }
        #endregion

        #region Constraint Members

        public override bool Matches<T>(ActualValueDelegate<T> del)
        {
            try
            {
                var response = del();
                actual = "SUCCESS";
                return false; // the delegate must throw a ResourceException to indicate failure
            }
            catch (ResourceException ex)
            {
                actual = ex.Status;
                return ex.Status == Status;
            }
        }

        public override bool Matches(object actual)
        {
            if (actual is TestDelegate)
            {
                return Matches(() => { ((TestDelegate)actual)(); return false; });
            }
            return false;
        }

        public override void WriteDescriptionTo(MessageWriter writer)
        {
            writer.Write(Status);
        }
        #endregion
    }

    public class ResourceException : Exception
    {
        #region Public Properties

        public int Status { get; private set; }

        #endregion

        #region Constructor

        public ResourceException(int status, string message)
            : base(message)
        {
            Status = status;
        }
        #endregion
    }
}
