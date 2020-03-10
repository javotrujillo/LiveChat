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
using System.Net.WebSockets;
using Newtonsoft.Json;
using PureCloudPlatform.Client.V2.Extensions;

namespace LiveChat.Controllers
{
    public class ChatController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        public Client client { get; set; }
        private PureCloudPlatform.Client.V2.Client.Configuration configuration { get; set; }
        private ApiClient apiclient { get; set; }
        private WebChatApi webChatApi { get; set; }
        private Purecloudconfiguration pcconfiguration { get; set; }
        private CreateWebChatConversationResponse chatInfo { get; set; }
        private TokenResponse accessTokenInfo { get; set; }
        private IConfiguration _purecloudconfiguration;
        private ClientWebSocket clientWebSocket { get; set; }
        private System.Threading.CancellationToken CancellationToken { get; set; }


        public ChatController(IConfiguration purecloudconfiguration, ILogger<HomeController> logger)
        {
            _purecloudconfiguration = purecloudconfiguration;
            _logger = logger;

            //WebChat objects
            configuration = new PureCloudPlatform.Client.V2.Client.Configuration();
            pcconfiguration = new Purecloudconfiguration() { integrations = new integrations() };
            webChatApi = new WebChatApi();
            apiclient = new ApiClient();
            chatInfo = new CreateWebChatConversationResponse();
            clientWebSocket = new ClientWebSocket();
            accessTokenInfo = new TokenResponse();
        }

        [HttpPost]
        [Route("Chat/Index")]
        public async Task<IActionResult> IndexAsync()
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

            AuthTokenInfo accessTokenInfo = configuration.ApiClient.PostToken(
                pcconfiguration.integrations.credentials.client_id,
                pcconfiguration.integrations.credentials.client_secret);
            accessTokenInfo.AccessToken = accessTokenInfo.AccessToken;
            accessTokenInfo.TokenType = accessTokenInfo.TokenType;

            CreateWebChatConversationRequest chatbody = new CreateWebChatConversationRequest()
            {
                DeploymentId = pcconfiguration.integrations.deployment.id,
                OrganizationId = pcconfiguration.integrations.organization.id,
                RoutingTarget = new WebChatRoutingTarget()
                {
                    Language = pcconfiguration.integrations.others.language,
                    TargetType = WebChatRoutingTarget.TargetTypeEnum.Queue,
                    TargetAddress = client.queuename.ToString(),
                    Skills = new List<string>() { client.queuename.ToString() },
                    Priority = 5

                },
                MemberInfo = new GuestMemberInfo()
                {
                    DisplayName = client.firstname + " " + client.lastname,
                    CustomFields = new Dictionary<string, string>()
                    {
                        { "Dirección IP","192.168.0.1" },
                        { "","" },
                        { "phoneNumber", client.customerid.ToString() },
                        { "customField1Label", "Direccion IP 2"},
                        { "customField1", "10.0.0.1"},
                        { "customField2Label", ""},
                        { "customField2", ""},
                        { "customField3Label", ""},
                        { "customField3", ""},
                    },
                    AvatarImageUrl = @"https://d3a63qt71m2kua.cloudfront.net/developer-tools/1554/assets/images/PC-blue-nomark.png"

                }
            };

            chatInfo = await webChatApi.PostWebchatGuestConversationsAsync(chatbody);
            chatInfo.Member.Role = WebChatMemberInfo.RoleEnum.Customer;
            chatInfo.Member.State = WebChatMemberInfo.StateEnum.Connected;

            ViewBag.chatinformation = chatInfo;
            ViewBag.chatbody = chatbody;
            ViewBag.client = client;
            ViewBag.configuration = configuration;

            return View(configuration);
        }

        private Task Receive(ClientWebSocket socket)
        {
            throw new NotImplementedException();
        }

        private Task Send(ClientWebSocket socket, string v)
        {
            throw new NotImplementedException();
        }

        [Route("Chat/LogOut")]
        public IActionResult LogOut()
        {
            return RedirectToAction("Index", "home");
        }

        [HttpGet]
        public JsonResult SendMessage(string messagetosend, string chatInfoId, string MemberId, Configuration configuration)
        {
            CreateWebChatMessageRequest message = new CreateWebChatMessageRequest() { Body = messagetosend, BodyType = CreateWebChatMessageRequest.BodyTypeEnum.Standard };
            //var test = webChatApi.PostWebchatGuestConversationMemberMessagesAsync(chatInfo.Id, chatInfo.Member.Id, message);
            webChatApi.PostWebchatGuestConversationMemberMessages(chatInfoId, MemberId, message);
            var json = JsonConvert.SerializeObject(message);
            return Json(json);
        }




    }
}