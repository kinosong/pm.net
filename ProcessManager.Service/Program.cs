using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using ProcessManager.Service.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace ProcessManager.Service
{
    public class Program
    {
        public static void Main(string[] args)
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };

            var host = BuildWebHost(args);

            if (Debugger.IsAttached || args.Contains("--debug"))
            {
                ApplicationManager.Instance.StartAll();
                host.Run();
            }
            else
            {
                host.RunAsService();
            }
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .Build();
    }
}
