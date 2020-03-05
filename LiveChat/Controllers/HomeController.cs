using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using LiveChat.Models;
using PureCloudPlatform.Client.V2.Client;
using PureCloudPlatform.Client.V2.Api;
using PureCloudPlatform.Client.V2.Model;
using System.Xml;
using Microsoft.Extensions.Configuration;

namespace LiveChat.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        public Client client { get; set; }
        private Configuration configuration { get; set; }
        private ApiClient apiclient { get; set; }
        private WebChatApi webChatApi { get; set; }
        private Purecloudconfiguration purecloudconfiguration { get; set; }

        private IConfiguration _purecloudconfiguration;


        //public HomeController(ILogger<HomeController> logger)
        //{
        //    _logger = logger;
        //}

        public HomeController(IConfiguration purecloudconfiguration)
        {
            _purecloudconfiguration = purecloudconfiguration;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Chat()
        {
            var fullname = Request.Form["name"].ToString().Split(' ');
            client = new Client() {
                firstname = fullname[0],
                lastname = (fullname.Length > 1 ) ? fullname[1] : "",
                customerid = Request.Form["customerid"],
                queuename = Request.Form["queuename"]
            };
            //ViewData["name"] = @"Request.Form['name']" + " " + Request.Form["name"];

            XmlDocument integrationxml = new XmlDocument();
            XmlDocument organizationxml = new XmlDocument();
            integrationxml.Load(@"~/content/integration.xml");
            organizationxml.Load(@"~/content/organization.xml");

            XmlNodeList integrationnodes = integrationxml.SelectNodes("integrations");
            XmlNodeList organizationnodes = organizationxml.SelectNodes("organization");

            purecloudconfiguration = new Purecloudconfiguration()
            {
                client_id = integrationnodes.Item(0).ChildNodes.Item(0).Attributes.GetNamedItem("client_id").Value,
                client_secret = integrationnodes.Item(0).ChildNodes.Item(0).Attributes.GetNamedItem("client_secret").Value,
                grant_type = integrationnodes.Item(0).ChildNodes.Item(4).Attributes.GetNamedItem("grant_type").Value,
                content_type = integrationnodes.Item(0).ChildNodes.Item(4).Attributes.GetNamedItem("content_type").Value,
                url_host = integrationnodes.Item(0).ChildNodes.Item(1).Attributes.GetNamedItem("host").Value,
                url_login = integrationnodes.Item(0).ChildNodes.Item(1).Attributes.GetNamedItem("login").Value,
                url_api = integrationnodes.Item(0).ChildNodes.Item(1).Attributes.GetNamedItem("api").Value,
                uri_token = integrationnodes.Item(0).ChildNodes.Item(2).Attributes.GetNamedItem("uritoken").Value,
                deploymentid = integrationnodes.Item(0).ChildNodes.Item(3).Attributes.GetNamedItem("id").Value,
                organizationid = organizationnodes.Item(0).ChildNodes.Item(0).Attributes.GetNamedItem("organizationid").Value,
                region = organizationnodes.Item(0).ChildNodes.Item(0).Attributes.GetNamedItem("region").Value
            };

            PureCloudRegionHosts region = PureCloudRegionHosts.us_east_1;
            configuration.ApiClient.setBasePath(region);
            //Configuration.Default.ApiClient.setBasePath(region);

            //var accessTokenInfo = configuration.ApiClient.PostToken(
            //    purecloudconfiguration.client_id,
            //    purecloudconfiguration.client_secret);
            ////var accessTokenInfo = Configuration.Default.ApiClient.PostToken(
            ////    purecloudconfiguration.client_id,
            ////    purecloudconfiguration.client_secret);

            //configuration.AccessToken = accessTokenInfo.AccessToken;
            ////Configuration.Default.AccessToken = accessTokenInfo.AccessToken;

            webChatApi = new WebChatApi();
            CreateWebChatConversationRequest chatbody = new CreateWebChatConversationRequest()
            {
                DeploymentId = purecloudconfiguration.deploymentid,
                OrganizationId = purecloudconfiguration.organizationid,
                RoutingTarget = new WebChatRoutingTarget()
                {
                    Language = "en",
                    TargetType = WebChatRoutingTarget.TargetTypeEnum.Queue,
                    TargetAddress = Request.Form["queuename"],
                    Skills = new List<string>(),
                    Priority = '5'
                },
                MemberInfo = new GuestMemberInfo()
                {
                    DisplayName = Request.Form["name"],
                    CustomFields = new Dictionary<string, string>() {
                        { "customField1Label", client.customfield1 },
                        { "customField2Label", client.customfield2 },
                        { "customField3Label", client.customfield3 }
                    }
                }
            };

            CreateWebChatConversationResponse chatInfo = webChatApi.PostWebchatGuestConversations(chatbody);

            return View(client);
        }

        public IActionResult LogOut()
        {
            return RedirectToAction("Index");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
