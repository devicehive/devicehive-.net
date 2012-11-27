using System;
using System.Collections.Generic;
using DeviceHive.Data.Repositories;
using DeviceHive.Data.EF;
using Ninject;

namespace DeviceHive.API
{
    public class DataContext
    {
        private IKernel _kernel;

        public DataContext(IKernel kernel)
        {
            _kernel = kernel;
        }

        public ISimpleRepository<T> Get<T>()
        {
            return _kernel.Get<ISimpleRepository<T>>();
        }

        public IUserRepository User
        {
            get { return _kernel.Get<IUserRepository>(); }
        }

        public IUserNetworkRepository UserNetwork
        {
            get { return _kernel.Get<IUserNetworkRepository>(); }
        }

        public INetworkRepository Network
        {
            get { return _kernel.Get<INetworkRepository>(); }
        }

        public IDeviceClassRepository DeviceClass
        {
            get { return _kernel.Get<IDeviceClassRepository>(); }
        }

        public IEquipmentRepository Equipment
        {
            get { return _kernel.Get<IEquipmentRepository>(); }
        }

        public IDeviceRepository Device
        {
            get { return _kernel.Get<IDeviceRepository>(); }
        }

        public IDeviceNotificationRepository DeviceNotification
        {
            get { return _kernel.Get<IDeviceNotificationRepository>(); }
        }

        public IDeviceCommandRepository DeviceCommand
        {
            get { return _kernel.Get<IDeviceCommandRepository>(); }
        }

        public IDeviceEquipmentRepository DeviceEquipment
        {
            get { return _kernel.Get<IDeviceEquipmentRepository>(); }
        }
    }
}