using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ProcessManager.Service.Models;
using Microsoft.AspNetCore.Http;

namespace ProcessManager.Service.Controllers
{
    [Route("api/apps")]
    public class ApplicationController : Controller
    {
        private ApplicationManager Manager => ApplicationManager.Instance;

        [HttpGet]
        public IEnumerable<Application> Get()
        {
            return Manager.Instances.Values.Select(v => v.Application);
        }

        [HttpGet("{name}")]
        public Application Get(string name)
        {
            return Manager.Instances[name].Application;
        }

        [HttpPost]
        public void Post([FromBody]Application app)
        {
            app.Status = ApplicationStatus.Offline;
            Manager.Add(app);
            Manager.Save();
        }

        [HttpPut("{name}")]
        public void Put(string name, [FromBody]Application app)
        {
            var local = Manager.Instances[name].Application;
            if (local.Status != app.Status)
            {
                if (app.Status == ApplicationStatus.Online && local.Status == ApplicationStatus.Offline)
                    Manager.Start(name);
                else if (app.Status == ApplicationStatus.Offline && local.Status == ApplicationStatus.Online)
                    Manager.Stop(name);
            }
        }

        [HttpDelete("{name}")]
        public void Delete(string name)
        {
            Manager.Remove(name);
            Manager.Save();
        }

        [HttpPost("{name}/pack")]
        public IActionResult Upload(string name, [FromForm]IFormFile file, [FromForm]bool full)
        {
            var local = Manager.Instances[name];
            if (local == null)
                return NotFound();

            local.Update(file.OpenReadStream(), full);
            return Ok();
        }
    }
}
