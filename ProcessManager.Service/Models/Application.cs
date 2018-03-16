using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;

namespace ProcessManager.Service.Models
{
    public class Application
    {
        public string Name { get; set; }
        public ApplicationStatus Status { get; set; } = ApplicationStatus.Offline;
        public string Description { get; set; }
        public string Path { get; set; }
        public string Urls { get; set; }
        public int Pid { get; set; }
        public int Memory { get; set; }
        public int Restarts { get; set; }
        public int UnstableRestarts { get; set; }
        public TimeSpan Uptime { get; set; }
        public DateTime CreatedAt { get; set; }

        [JsonIgnore]
        public Process Process { get; set; }
        [JsonIgnore]
        public FileSystemWatcher Watcher { get; set; }
    }
    
    public enum ApplicationStatus
    {
        Offline, Online, Starting, Stopping
    }
}
