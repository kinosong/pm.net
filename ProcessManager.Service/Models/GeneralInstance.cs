using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProcessManager.Service.Models
{
    public class GeneralInstance : ApplicationInstance
    {
        public GeneralInstance(Application app) : base(app)
        {
        }

        public override void Start()
        {
            Application.Status = ApplicationStatus.Starting;
            Process = System.Diagnostics.Process.Start(Application.Path);
            Application.Pid = Process.Id;
            Application.Status = ApplicationStatus.Online;
        }

        public override void Stop()
        {
            Application.Status = ApplicationStatus.Stopping;
            if (Process != null)
                Process.Kill();
            Application.Pid = 0;
            Application.Status = ApplicationStatus.Offline;
        }
    }
}
