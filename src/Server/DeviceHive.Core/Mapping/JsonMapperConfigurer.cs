using System;
using Ninject;

namespace DeviceHive.Core.Mapping
{
    /// <summary>
    /// Includes extensions methods for configuring json mapping
    /// </summary>
    public static class JsonMapperConfigurer
    {
        #region Public Methods

        /// <summary>
        /// Configures object to json mapping
        /// </summary>
        /// <typeparam name="T">Domain object type</typeparam>
        /// <param name="kernel">Ninject kernel object</param>
        /// <returns>JsonMapperConfiguration for further mapping customization</returns>
        public static JsonMapperConfiguration<T> ConfigureMapping<T>(this IKernel kernel)
        {
            // use default JsonMapper<T> implementation
            return kernel.ConfigureMapping<T, JsonMapper<T>>();
        }

        /// <summary>
        /// Configures object to json mapping
        /// </summary>
        /// <typeparam name="T">Domain object type</typeparam>
        /// <typeparam name="TMapper">Mapper object type</typeparam>
        /// <param name="kernel">Ninject kernel object</param>
        /// <returns>JsonMapperConfiguration for further mapping customization</returns>
        public static JsonMapperConfiguration<T> ConfigureMapping<T, TMapper>(this IKernel kernel)
            where TMapper : JsonMapper<T>
        {
            if (kernel == null)
                throw new ArgumentNullException("kernel");

            // create and rebind mapping configuration
            var configuration = kernel.Get<JsonMapperConfiguration<T>>();
            kernel.Rebind<JsonMapperConfiguration<T>>().ToConstant(configuration);

            // create and rebind mapper object
            var mapper = kernel.Get<TMapper>();
            kernel.Rebind<IJsonMapper<T>>().ToConstant(mapper);

            // add mapper to the manager instance
            var manager = kernel.Get<JsonMapperManager>();
            manager.AddMapper<T>(mapper);

            // return configuration object for further customization
            return configuration;
        }
        #endregion
    }
}