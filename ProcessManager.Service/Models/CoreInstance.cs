using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;

namespace ProcessManager.Service.Models
{
    public class CoreInstance : ApplicationInstance
    {
        private FileSystemWatcher Watcher { get; set; }

        public CoreInstance(Application app) : base(app)
        {
        }

        public override void Start()
        {
            var proc = GetProccess();
            if (proc != null)
            {
                Application.Status = ApplicationStatus.Online;
                Application.Pid = proc.Id;
                Process = proc;
                return;
            }

            Application.Status = ApplicationStatus.Starting;

            // copy app runtime to temp folder
            var dir = Path.GetDirectoryName(Application.Path);
            var tempDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp", Application.Name);
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
            var logPath = Path.Combine(logDir, $"{Application.Name}.log");

            var info = new ProcessStartInfo { WorkingDirectory = dir, FileName = "dotnet", Arguments = Path.Combine(tempDir, Path.GetFileName(Application.Path)) };
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

            Process = proc;
            Application.Pid = proc.Id;
            Application.Status = ApplicationStatus.Online;

            var watcher = new FileSystemWatcher(dir, "*.dll")
            {
                EnableRaisingEvents = true
            };
            Observable.FromEventPattern<FileSystemEventArgs>(watcher, "Changed").Throttle(TimeSpan.FromSeconds(10)).Subscribe(e =>
            {
                Stop();
                Start();
                Application.Restarts++;
            });
            Watcher = watcher;
        }

        private Process GetProccess()
        {
            return Process.GetProcessesByName("dotnet").FirstOrDefault(p => p.Id == Application.Pid);
        }

        public override void Stop()
        {
            Application.Status = ApplicationStatus.Stopping;
            Process.Kill();

            Application.Pid = 0;
            Process = null;
            Application.Status = ApplicationStatus.Offline;
            Watcher.Dispose();
        }

        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            var dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            var dirs = dir.GetDirectories();
            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            var files = dir.GetFiles();
            foreach (var file in files)
            {
                var temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (var subdir in dirs)
                {
                    var temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }
    }
}
