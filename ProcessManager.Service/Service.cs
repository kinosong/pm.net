using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.WindowsServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace ProcessManager.Service
{
    internal class Service : WebHostService
    {
        public Service(IWebHost host) : base(host)
        {
        }

        protected override void OnStarting(string[] args)
        {
            base.OnStarting(args);
        }

        protected override void OnStarted()
        {
            base.OnStarted();
        }

        protected override void OnStopping()
        {
            base.OnStopping();
        }
    }

    public static class ServiceExtensions
    {
        public static void RunAsService(this IWebHost host)
        {
            var webHostService = new Service(host);
            ServiceBase.Run(webHostService);
        }
    }
}
