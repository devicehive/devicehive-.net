using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Web;
using System.Web.Http;
using System.Web.Http.Dependencies;
using System.Web.Routing;
using Ninject;
using Ninject.Components;
using Ninject.Syntax;
using Ninject.Web.Common;

namespace DeviceHive.API
{
    public class NinjectDependencyScope : IDependencyScope
    {
        private readonly IResolutionRoot _resolutionRoot;

        #region Constructor

        public NinjectDependencyScope(IResolutionRoot resolutionRoot)
        {
            _resolutionRoot = resolutionRoot;
        }
        #endregion

        #region IDependencyScope Members

        public object GetService(Type serviceType)
        {
            return _resolutionRoot.TryGet(serviceType);
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            return _resolutionRoot.GetAll(serviceType);
        }

        public void Dispose()
        {
            // intentionally left empty - handled by Ninject not by ASP.NET
        }
        #endregion
    }


    public class NinjectDependencyResolver : NinjectDependencyScope, IDependencyResolver
    {
        #region Constructor

        public NinjectDependencyResolver(IKernel kernel)
            : base(kernel)
        {
        }
        #endregion

        #region IDependencyResolver Members

        public IDependencyScope BeginScope()
        {
            return this;
        }
        #endregion
    }

    public class NinjectWebApiHttpApplicationPlugin : NinjectComponent, INinjectHttpApplicationPlugin
    {
        private readonly IKernel _kernel;

        #region Constructor

        public NinjectWebApiHttpApplicationPlugin(IKernel kernel)
        {
            _kernel = kernel;
        }
        #endregion

        #region INinjectHttpApplicationPlugin Members

        public object RequestScope
        {
            get { return HttpContext.Current; }
        }

        public void Start()
        {
            GlobalConfiguration.Configuration.DependencyResolver = CreateDependencyResolver();
        }

        public void Stop()
        {
        }
        #endregion

        #region Protected Methods

        protected IDependencyResolver CreateDependencyResolver()
        {
            return _kernel.Get<IDependencyResolver>();
        }
        #endregion
    }

    public class WebApiModule : GlobalKernelRegistrationModule<OnePerRequestHttpModule>
    {
        #region NinjectModule Members

        public override void Load()
        {
            base.Load();

            this.Kernel.Components.Add<INinjectHttpApplicationPlugin, NinjectWebApiHttpApplicationPlugin>();
            this.Kernel.Bind<IDependencyResolver>().To<NinjectDependencyResolver>();
            this.Kernel.Bind<RouteCollection>().ToConstant(RouteTable.Routes);
            this.Kernel.Bind<HttpContext>().ToMethod(ctx => HttpContext.Current).InTransientScope();
            this.Kernel.Bind<HttpContextBase>().ToMethod(ctx => new HttpContextWrapper(HttpContext.Current)).InTransientScope();
        }
        #endregion
    }
}