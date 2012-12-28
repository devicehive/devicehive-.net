using System;
using System.Collections.Generic;
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
        private Dictionary<Type, Type> _repositoryTypeMap;
        private Dictionary<Type, Type> _objectTypeMap;

        #region Constructor

        /// <summary>
        /// Registers all repositories in the current assembly
        /// </summary>
        public DataContext()
            : this(Assembly.GetExecutingAssembly())
        {
        }

        /// <summary>
        /// Registers all repositories in the specified assembly
        /// </summary>
        /// <param name="repositoriesAssembly">Assembly containing repository implementations</param>
        public DataContext(Assembly repositoriesAssembly)
            : this(repositoriesAssembly.GetTypes())
        {
        }

        /// <summary>
        /// Registers all specified repositories
        /// </summary>
        /// <param name="repositories">Array of repository types to register</param>
        public DataContext(params Type[] repositories)
        {
            var repositoriesInfo = repositories.Where(t => t.IsClass).Select(t => new
                {
                    Implementation = t,
                    Repositories = t.GetInterfaces().Where(i => i.GetInterfaces().Any(ii => IsSimpleRepository(ii))).ToArray(),
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
        #endregion

        #region Public Methods

        /// <summary>
        /// Gets repository of the specified type
        /// </summary>
        /// <typeparam name="TRepository">Type of the repository</typeparam>
        /// <returns>Repository interface</returns>
        public TRepository GetRepository<TRepository>()
        {
            Type repositoryType;
            if (!_repositoryTypeMap.TryGetValue(typeof(TRepository), out repositoryType))
                throw new ArgumentException("Repository of the specified type does not exist! Type: " + typeof(TRepository));

            return (TRepository)CreateRepositoryInstance(repositoryType);
        }

        /// <summary>
        /// Gets repository for objects of the specified type
        /// </summary>
        /// <typeparam name="T">Type of the objects</typeparam>
        /// <returns>Repository interface</returns>
        public ISimpleRepository<T> GetRepositoryFor<T>()
        {
            Type repositoryType;
            if (!_objectTypeMap.TryGetValue(typeof(T), out repositoryType))
                throw new ArgumentException("Repository for objects of the specified type does not exist! Type: " + typeof(T));

            return (ISimpleRepository<T>)CreateRepositoryInstance(repositoryType);
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
        /// Gets repository for equipments
        /// </summary>
        public IEquipmentRepository Equipment
        {
            get { return GetRepository<IEquipmentRepository>(); }
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
        #endregion

        #region Protected Methods

        /// <summary>
        /// Creates a repository instance of the specified type
        /// </summary>
        /// <param name="repositoryType">Repository type</param>
        /// <returns>Repository instance</returns>
        protected virtual object CreateRepositoryInstance(Type repositoryType)
        {
            return Activator.CreateInstance(repositoryType);
        }
        #endregion

        #region Private Methods

        private bool IsSimpleRepository(Type t)
        {
            return t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ISimpleRepository<>);
        }
        #endregion
    }
}