using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProcessManager.Service.Models
{
    public class StaticInstance : ApplicationInstance
    {
        public override bool Guarding { get; } = false;

        public StaticInstance(Application app) : base(app)
        {
            app.Status = ApplicationStatus.Online;
        }

        public override void Start()
        {
        }

        public override void Stop()
        {
        }
    }
}
