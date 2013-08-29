using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using DeviceHive.Data.Repositories;

namespace DeviceHive.Data
{
    /// <summary>
    /// Represents data context providing access to repositories
    /// </summary>
    public class DataContext
    {
        private Func<Type, object> _instanceCreator;
        private Dictionary<Type, Type> _repositoryTypeMap;
        private Dictionary<Type, Type> _objectTypeMap;

        #region Public Properties

        /// <summary>
        /// Gets array or registered repository interface types
        /// </summary>
        public Type[] RegisteredInterfaces
        {
            get { return _repositoryTypeMap.Keys.ToArray(); }
        }

        /// <summary>
        /// Gets array of registered object types for simple repositories
        /// </summary>
        public Type[] RegisteredObjects
        {
            get { return _objectTypeMap.Keys.ToArray(); }
        }
        #endregion

        #region Constructor

        /// <summary>
        /// Registers all repositories in the assembly specified in the RepositoryAssembly application setting.
        /// </summary>
        public DataContext()
        {
            var repositoryAssembly = ConfigurationManager.AppSettings["RepositoryAssembly"];
            if (string.IsNullOrWhiteSpace(repositoryAssembly))
                throw new ArgumentNullException("Please specify the RepositoryAssembly setting in the configuration file!");

            var repositoryAssemblyObject = Assembly.Load(repositoryAssembly);
            LoadRepositories(repositoryAssemblyObject.GetTypes());
        }

        /// <summary>
        /// Registers all repositories in the specified assembly.
        /// </summary>
        /// <param name="repositoryAssembly">Assembly containing repository implementations.</param>
        public DataContext(Assembly repositoryAssembly)
            : this(repositoryAssembly.GetTypes())
        {
        }

        /// <summary>
        /// Registers all specified repositories.
        /// </summary>
        /// <param name="repositories">Array of repository types to register.</param>
        public DataContext(params Type[] repositories)
        {
            LoadRepositories(repositories);
        }
        #endregion

        #region Public Methods

        /// <summary>
        /// Gets repository for the specified interface type
        /// </summary>
        /// <typeparam name="TInterface">Interface type</typeparam>
        /// <returns>Repository implementation</returns>
        public TInterface GetRepository<TInterface>()
        {
            var repositoryType = GetRepositoryType(typeof(TInterface));
            return (TInterface)CreateRepositoryInstance(repositoryType);
        }

        /// <summary>
        /// Gets repository type for the specified interface type
        /// </summary>
        /// <param name="interfaceType">Repository interface type</param>
        /// <returns>Repository type</returns>
        public Type GetRepositoryType(Type interfaceType)
        {
            Type repositoryType;
            if (!_repositoryTypeMap.TryGetValue(interfaceType, out repositoryType))
                throw new ArgumentException("Repository for the specified interface type does not exist! Type: " + interfaceType);

            return repositoryType;
        }

        /// <summary>
        /// Gets repository for specified object type
        /// </summary>
        /// <typeparam name="TObject">Object type</typeparam>
        /// <returns>Repository implementation</returns>
        public ISimpleRepository<TObject> GetRepositoryFor<TObject>()
        {
            var repositoryType = GetRepositoryTypeFor(typeof(TObject));
            return (ISimpleRepository<TObject>)CreateRepositoryInstance(repositoryType);
        }

        /// <summary>
        /// Gets repository type for the specified objects type
        /// </summary>
        /// <param name="objectType">Object type</param>
        /// <returns>Repository type</returns>
        public Type GetRepositoryTypeFor(Type objectType)
        {
            Type repositoryType;
            if (!_objectTypeMap.TryGetValue(objectType, out repositoryType))
                throw new ArgumentException("Repository for objects of the specified type does not exist! Type: " + objectType);

            return repositoryType;
        }

        /// <summary>
        /// Sets delegate which will be used for repository instance creation
        /// </summary>
        /// <param name="instanceCreator">A delegate to create repository instances</param>
        public void SetRepositoryCreator(Func<Type, object> instanceCreator)
        {
            _instanceCreator = instanceCreator;
        }
        #endregion

        #region Repository Shortcuts

        /// <summary>
        /// Gets repository for users
        /// </summary>
        public IUserRepository User
        {
            get { return GetRepository<IUserRepository>(); }
        }

        /// <summary>
        /// Gets repository for access keys
        /// </summary>
        public IAccessKeyRepository AccessKey
        {
            get { return GetRepository<IAccessKeyRepository>(); }
        }

        /// <summary>
        /// Gets repository for user networks
        /// </summary>
        public IUserNetworkRepository UserNetwork
        {
            get { return GetRepository<IUserNetworkRepository>(); }
        }

        /// <summary>
        /// Gets repository for networks
        /// </summary>
        public INetworkRepository Network
        {
            get { return GetRepository<INetworkRepository>(); }
        }

        /// <summary>
        /// Gets repository for device classes
        /// </summary>
        public IDeviceClassRepository DeviceClass
        {
            get { return GetRepository<IDeviceClassRepository>(); }
        }

        /// <summary>
        /// Gets repository for devices
        /// </summary>
        public IDeviceRepository Device
        {
            get { return GetRepository<IDeviceRepository>(); }
        }

        /// <summary>
        /// Gets repository for device notifications
        /// </summary>
        public IDeviceNotificationRepository DeviceNotification
        {
            get { return GetRepository<IDeviceNotificationRepository>(); }
        }

        /// <summary>
        /// Gets repository for device commands
        /// </summary>
        public IDeviceCommandRepository DeviceCommand
        {
            get { return GetRepository<IDeviceCommandRepository>(); }
        }

        /// <summary>
        /// Gets repository for device equipments
        /// </summary>
        public IDeviceEquipmentRepository DeviceEquipment
        {
            get { return GetRepository<IDeviceEquipmentRepository>(); }
        }

        /// <summary>
        /// Gets repository for timestamp operations
        /// </summary>
        public ITimestampRepository Timestamp
        {
            get { return GetRepository<ITimestampRepository>(); }
        }
        #endregion

        #region Protected Methods

        /// <summary>
        /// Creates a repository instance of the specified type
        /// </summary>
        /// <param name="repositoryType">Repository type</param>
        /// <returns>Repository instance</returns>
        protected virtual object CreateRepositoryInstance(Type repositoryType)
        {
            if (_instanceCreator != null)
                return _instanceCreator(repositoryType);

            return Activator.CreateInstance(repositoryType);
        }
        #endregion

        #region Private Methods

        private void LoadRepositories(Type[] repositories)
        {
            var repositoriesInfo = repositories.Where(t => t.IsClass).Select(t => new
                {
                    Implementation = t,
                    Repositories = t.GetInterfaces().Where(i => i.Name.EndsWith("Repository") || i.GetInterfaces().Any(ii => IsSimpleRepository(ii))).ToArray(),
                    Objects = t.GetInterfaces().Where(i => IsSimpleRepository(i)).Select(i => i.GetGenericArguments().First()).ToArray(),
                }).ToArray();

            var duplicatedRepositories = repositoriesInfo.SelectMany(m => m.Repositories).GroupBy(r => r).Where(r => r.Count() > 1);
            if (duplicatedRepositories.Any())
                throw new Exception("Can not register multiple implementations for repositories: " + string.Join(", ", duplicatedRepositories.Select(r => r.Key.ToString())));

            var duplicatedObjects = repositoriesInfo.SelectMany(m => m.Objects).GroupBy(r => r).Where(r => r.Count() > 1);
            if (duplicatedObjects.Any())
                throw new Exception("Can not register multiple repositories for objects: " + string.Join(", ", duplicatedObjects.Select(r => r.Key.ToString())));

            _repositoryTypeMap = repositoriesInfo
                .SelectMany(m => m.Repositories.Select(r => new { Repository = r, Implementation = m.Implementation }))
                .ToDictionary(r => r.Repository, r => r.Implementation);

            _objectTypeMap = repositoriesInfo
                .SelectMany(m => m.Objects.Select(o => new { Object = o, Implementation = m.Implementation }))
                .ToDictionary(m => m.Object, m => m.Implementation);
        }

        private bool IsSimpleRepository(Type t)
        {
            return t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ISimpleRepository<>);
        }
        #endregion
    }
}