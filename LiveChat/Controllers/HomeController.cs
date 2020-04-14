using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using LiveChat.Models;

using System.Xml;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using System.Net.WebSockets;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LiveChat.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private IConfiguration _purecloudconfiguration;
        public Queues queues;
        public Purecloudconfiguration pcconfiguration { get; set; }

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index([FromServices] IConfiguration purecloudconfiguration)
        {
            pcconfiguration = new Purecloudconfiguration() { integrations = new integrations() };
            _purecloudconfiguration = purecloudconfiguration;
            _purecloudconfiguration.GetSection("integrations").Bind(pcconfiguration.integrations);

            queues = new Queues() { data = new Dictionary<int, string>() };

            var listqueues = from pair in pcconfiguration.integrations.queue.Values
                             orderby pair.index ascending
                             select pair;

            foreach (var item in listqueues)
            {
                queues.data.Add(item.index, pcconfiguration.integrations.queue.FirstOrDefault(x => x.Value.index == item.index).Key);
            }


            return View(queues);
        }

        //[HttpPost]
        //public ActionResult Chat()
        //{
        //    return RedirectToRoutePreserveMethod("Index", Request.Body);
        //}

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
