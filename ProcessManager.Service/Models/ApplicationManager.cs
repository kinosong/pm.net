using Newtonsoft.Json;
using ProcessManager.Service.Helpers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Reactive.Linq;

namespace ProcessManager.Service.Models
{
    public class ApplicationManager
    {
        private static ApplicationManager instance;
        public static ApplicationManager Instance => instance ?? (instance = new ApplicationManager());

        public ConcurrentDictionary<string, ApplicationInstance> Instances { get; private set; }

        public ApplicationManager()
        {
            Instances = new ConcurrentDictionary<string, ApplicationInstance>();
            Load();
            PeriodicTask.Run(MonitorProcess, TimeSpan.FromSeconds(5));
        }

        public void Add(Application app)
        {
            Instances[app.Name] = ApplicationInstance.Create(app);
        }

        public void Remove(string name)
        {
            Instances.Remove(name, out ApplicationInstance app);
        }

        public void StartAll()
        {
            foreach (var app in Instances)
            {
                app.Value.Start();
            }
            Save();
        }

        public void StopAll()
        {
            foreach (var app in Instances)
            {
                app.Value.Stop();
            }
            Save();
        }

        public void Start(string name)
        {
            if (Instances.TryGetValue(name, out var value))
            {
                value.Start();
                Save();
            }
        }

        public void Stop(string name)
        {
            if (Instances.TryGetValue(name, out var value))
            {
                value.Stop();
                Save();
            }
        }

        private void MonitorProcess()
        {
            foreach (var entry in Instances)
            {
                var instance = entry.Value;
                if (!instance.Guarding)
                    continue;

                var app = instance.Application;
                var proc = instance.Process;
                if (proc != null && app.Status != ApplicationStatus.Stopping)
                {
                    proc.Refresh();
                    if (proc.HasExited)
                    {
                        instance.Process = null;
                        app.Pid = 0;
                        app.Status = ApplicationStatus.Offline;
                        instance.Start();
                        app.Restarts++;
                        app.UnstableRestarts++;
                    }
                }
            }
        }

        private void Load()
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "applications.json");
            if (!File.Exists(path))
                return;

            var apps = JsonConvert.DeserializeObject<Application[]>(File.ReadAllText(path));
            foreach (var app in apps)
            {
                Add(app);
            }
        }

        public void Save()
        {
            lock (Instances)
            {
                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "applications.json");
                File.WriteAllText(path, JsonConvert.SerializeObject(Instances.Values.Select(v => v.Application).ToArray()));
            }
        }
    }
}
