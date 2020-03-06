using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LiveChat.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PureCloudPlatform.Client.V2.Client;
using PureCloudPlatform.Client.V2.Api;
using PureCloudPlatform.Client.V2.Model;
using Microsoft.Extensions.Configuration;

namespace LiveChat.Controllers
{
    public class ChatController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        public Client client { get; set; }
        private Configuration configuration { get; set; }
        private ApiClient apiclient { get; set; }
        private WebChatApi webChatApi { get; set; }
        private Purecloudconfiguration pcconfiguration { get; set; }
        private CreateWebChatConversationResponse chatInfo { get; set; }

        private IConfiguration _purecloudconfiguration;

        public ChatController(IConfiguration purecloudconfiguration, ILogger<HomeController> logger)
        {
            _purecloudconfiguration = purecloudconfiguration;
            _logger = logger;

            //WebChat objects
            configuration = new Configuration();
            pcconfiguration = new Purecloudconfiguration() { integrations = new integrations() };
            webChatApi = new WebChatApi();
            apiclient = new ApiClient();
            chatInfo = new CreateWebChatConversationResponse();
        }

        // GET: Chat
        [HttpPost]
        public IActionResult Index()
        {
            var fullname = Request.Form["name"].ToString().Split(' ');
            client = new Client()
            {
                firstname = fullname[0],
                lastname = (fullname.Length > 1) ? fullname[1] : "",
                customerid = Request.Form["customerid"],
                queuename = Request.Form["queuename"]
            };
            _purecloudconfiguration.GetSection("integrations").Bind(pcconfiguration.integrations);
            PureCloudRegionHosts region = PureCloudRegionHosts.us_east_1;
            configuration.ApiClient.setBasePath(region);

            //Configuration.Default.ApiClient.setBasePath(region);
            //var accessTokenInfo = configuration.ApiClient.PostToken(
            //    pcconfiguration.integrations.credentials.client_id,
            //    pcconfiguration.integrations.credentials.client_secret);
            ////var accessTokenInfo = Configuration.Default.ApiClient.PostToken(
            ////    pcconfiguration.integrations.credentials.client_id,
            ////    pcconfiguration.integrations.credentials.client_secret);
            //configuration.AccessToken = accessTokenInfo.AccessToken;
            ////Configuration.Default.AccessToken = accessTokenInfo.AccessToken;

            CreateWebChatConversationRequest chatbody = new CreateWebChatConversationRequest()
            {
                DeploymentId = pcconfiguration.integrations.deployment.id,
                OrganizationId = pcconfiguration.integrations.organization.id,
                RoutingTarget = new WebChatRoutingTarget()
                {
                    Language = pcconfiguration.integrations.others.language,
                    TargetType = WebChatRoutingTarget.TargetTypeEnum.Queue,
                    TargetAddress = client.queuename,
                    Skills = new List<string>() { client.queuename },
                    Priority = '5'
                },
                MemberInfo = new GuestMemberInfo()
                {
                    DisplayName = client.firstname + " " + client.lastname,
                    CustomFields = new Dictionary<string, string>() {
                        { "customField1Label", client.customfield1 },
                        { "customField2Label", client.customfield2 },
                        { "customField3Label", client.customfield3 }
                    }
                }
            };
            chatInfo = webChatApi.PostWebchatGuestConversations(chatbody);
            ChatInfo chat = new ChatInfo() { streamUri = chatInfo.EventStreamUri };
            return View("Index", chat.streamUri);
        }

        public IActionResult SendMessage()
        {
            CreateWebChatMessageRequest message = new CreateWebChatMessageRequest() { Body ="Fuck you", BodyType = CreateWebChatMessageRequest.BodyTypeEnum.Standard };
            webChatApi.PostWebchatGuestConversationMemberMessagesAsync(chatInfo.Id, chatInfo.Member.Id, message);
            return Ok();
        }

        public IActionResult LogOut()
        {
            return RedirectToAction("Index", "home");
        }

        // POST: Chat/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(IFormCollection collection)
        {
            try
            {
                // TODO: Add insert logic here

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: Chat/Edit/5
        public IActionResult Edit(int id)
        {
            return View();
        }

        // POST: Chat/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                // TODO: Add update logic here

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: Chat/Delete/5
        public IActionResult Delete(int id)
        {
            return View();
        }

        // POST: Chat/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                // TODO: Add delete logic here

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}