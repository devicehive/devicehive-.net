using System;
using DeviceHive.WebSockets.ActionsFramework;
using Ninject;

namespace DeviceHive.WebSockets.Core
{
    public class NinjectRouter : Router
    {
        private readonly IKernel _kernel;

        public NinjectRouter(IKernel kernel)
        {
            _kernel = kernel;
        }

        protected override object CreateController(Type type)
        {
            return _kernel.Get(type);
        }
    }
}