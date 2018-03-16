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

        public ConcurrentDictionary<string, Application> Applications { get; private set; }

        public ApplicationManager()
        {
            Applications = new ConcurrentDictionary<string, Application>();
            Load();
            PeriodicTask.Run(MonitorProcess, TimeSpan.FromSeconds(5));
        }

        public void Add(Application app)
        {
            Applications[app.Name] = app;
        }

        public void Remove(string name)
        {
            Applications.Remove(name, out Application app);
        }

        public void StartAll()
        {
            foreach (var app in Applications)
            {
                Start(app.Value);
            }
        }

        public void StopAll()
        {
            foreach (var app in Applications)
            {
                Stop(app.Value);
            }
        }

        public void Start(string name)
        {
            Start(Applications[name]);
        }

        public void Stop(string name)
        {
            Stop(Applications[name]);
        }

        private void Start(Application app)
        {
            if (app == null)
                return;

            var proc = GetProccess(app);
            if (proc != null)
            {
                app.Status = ApplicationStatus.Online;
                app.Pid = proc.Id;
                app.Process = proc;
                Save();
                return;
            }

            app.Status = ApplicationStatus.Starting;

            // copy app runtime to temp folder
            var dir = Path.GetDirectoryName(app.Path);
            var tempDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp", app.Name);
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
            Directory.CreateDirectory(tempDir);
            DirectoryCopy(dir, tempDir, false);
            var runtimeDir = Path.Combine(tempDir, "runtimes");
            Directory.CreateDirectory(runtimeDir);
            DirectoryCopy(Path.Combine(dir, "runtimes"), runtimeDir, true);
            
            var logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            if (!Directory.Exists(logDir))
                Directory.CreateDirectory(logDir);
            var logPath = Path.Combine(logDir, $"{app.Name}.log");

            var info = new ProcessStartInfo { WorkingDirectory = dir, FileName = "dotnet", Arguments = Path.Combine(tempDir, Path.GetFileName(app.Path)) };
            info.UseShellExecute = false;
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;
            info.CreateNoWindow = true;
            proc = Process.Start(info);
            proc.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    File.AppendAllText(logPath, e.Data + Environment.NewLine);
            };
            proc.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    File.AppendAllText(logPath, e.Data + Environment.NewLine);
            };
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();

            app.Pid = proc.Id;
            app.Process = proc;
            app.Status = ApplicationStatus.Online;
            Save();

            var watcher = new FileSystemWatcher(dir, "*.dll")
            {
                EnableRaisingEvents = true
            };
            Observable.FromEventPattern<FileSystemEventArgs>(watcher, "Changed").Throttle(TimeSpan.FromSeconds(10)).Subscribe(e =>
            {
                Stop(app);
                Start(app);
                app.Restarts++;
            });
            app.Watcher = watcher;
        }

        private void Stop(Application app)
        {
            if (app == null)
                return;

            app.Status = ApplicationStatus.Stopping;
            app.Process.Kill();

            app.Pid = 0;
            app.Process = null;
            app.Status = ApplicationStatus.Offline;
            app.Watcher.Dispose();
            Save();
        }

        private Process GetProccess(Application app)
        {
            return Process.GetProcessesByName("dotnet").FirstOrDefault(p => p.Id == app.Pid);
        }

        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();
            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }

        private void MonitorProcess()
        {
            foreach (var entry in Applications)
            {
                var app = entry.Value;
                var proc = app.Process;
                if (proc != null && app.Status != ApplicationStatus.Stopping)
                {
                    proc.Refresh();
                    if (proc.HasExited)
                    {
                        app.Pid = 0;
                        app.Process = null;
                        app.Status = ApplicationStatus.Offline;
                        Start(app);
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
            lock (Applications)
            {
                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "applications.json");
                File.WriteAllText(path, JsonConvert.SerializeObject(Applications.Values.ToArray()));
            }
        }
    }
}
