using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;

namespace ProcessManager.Service.Models
{
    public abstract class ApplicationInstance
    {
        public Application Application { get; private set; }
        public virtual bool Guarding { get; } = true;
        public Process Process { get; set; }

        public ApplicationInstance(Application app)
        {
            Application = app;
        }

        public static ApplicationInstance Create(Application app)
        {
            switch (app.Type)
            {
                case ApplicationType.Core:
                    return new CoreInstance(app);
                case ApplicationType.General:
                    return new GeneralInstance(app);
                case ApplicationType.Static:
                    return new StaticInstance(app);
                default:
                    throw new NotSupportedException(app.Type.ToString());
            }
        }

        public abstract void Start();
        public abstract void Stop();
        
        public void Update(Stream stream, bool full)
        {
            bool online = false;
            if (Application.Status == ApplicationStatus.Online)
            {
                online = true;
                Stop();
            }

            if (full)
            {
                Directory.Delete(Application.Directory, true);
                Directory.CreateDirectory(Application.Directory);
            }

            using (var zip = new ZipArchive(stream))
            {
                foreach (var entry in zip.Entries)
                {
                    entry.ExtractToFile(Path.Combine(Application.Directory, entry.FullName), true);
                }
            }

            if (online)
                Start();
        }
    }
}
