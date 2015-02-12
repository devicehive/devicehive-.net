using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceHive.Client
{
    /// <summary>
    /// Represents extensions to the <see cref="IRestClient"/> interface.
    /// </summary>
    public static class RestClientExtensions
    {
        /// <summary>
        /// Invokes a HTTP GET operation.
        /// </summary>
        /// <typeparam name="T">Resource type.</typeparam>
        /// <param name="restClient">A instance implementing the <see cref="IRestClient"/> interface.</param>
        /// <param name="url">Relative URL to the resource.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A resource returned from the server.</returns>
        public static async Task<T> GetAsync<T>(this IRestClient restClient, string url, CancellationToken? cancellationToken = null)
            where T : class
        {
            return await restClient.InvokeAsync<T>(HttpMethod.Get, url, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Invokes a HTTP POST operation.
        /// </summary>
        /// <typeparam name="T">Resource type.</typeparam>
        /// <param name="restClient">A instance implementing the <see cref="IRestClient"/> interface.</param>
        /// <param name="url">Relative URL to the resource.</param>
        /// <param name="content">Resource to send.</param>
        /// <returns>A resource returned from the server.</returns>
        public static async Task<T> PostAsync<T>(this IRestClient restClient, string url, T content)
            where T : class
        {
            return await restClient.InvokeAsync<T>(HttpMethod.Post, url, content);
        }

        /// <summary>
        /// Invokes a HTTP PUT operation.
        /// </summary>
        /// <typeparam name="T">Resource type.</typeparam>
        /// <param name="restClient">A instance implementing the <see cref="IRestClient"/> interface.</param>
        /// <param name="url">Relative URL to the resource.</param>
        /// <param name="content">Resource to send.</param>
        /// <returns>A <see cref="Task"/> object.</returns>
        public static async Task PutAsync<T>(this IRestClient restClient, string url, T content)
            where T : class
        {
            await restClient.InvokeAsync<T>(HttpMethod.Put, url, content);
        }

        /// <summary>
        /// Invokes a HTTP DELETE operation.
        /// </summary>
        /// <param name="restClient">A instance implementing the <see cref="IRestClient"/> interface.</param>
        /// <param name="url">Relative URL to the resource.</param>
        /// <returns>A <see cref="Task"/> object.</returns>
        public static async Task DeleteAsync(this IRestClient restClient, string url)
        {
            await restClient.InvokeAsync<object>(HttpMethod.Delete, url);
        }
    }
}
