using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceHive.Client
{
    /// <summary>
    /// Represents a REST client to the DeviceHive server.
    /// </summary>
    public interface IRestClient
    {
        /// <summary>
        /// Invokes a HTTP method on the DeviceHive server.
        /// </summary>
        /// <typeparam name="T">Resource type.</typeparam>
        /// <param name="method">HTTP method.</param>
        /// <param name="url">Relative URL to the resource.</param>
        /// <param name="content">Resource to send (for POST and PUT methods).</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A resource returned from the server (for GET and POST methods).</returns>
        Task<T> InvokeAsync<T>(HttpMethod method, string url, T content = null, CancellationToken? cancellationToken = null)
            where T : class;
    }
}
