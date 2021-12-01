using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using LiveChat.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PureCloudPlatform.Client.V2.Api;
using PureCloudPlatform.Client.V2.Client;
using PureCloudPlatform.Client.V2.Extensions;
using PureCloudPlatform.Client.V2.Model;

namespace LiveChat.Controllers
{
    public class ChatController : Microsoft.AspNetCore.Mvc.Controller
    {
        private readonly ILogger<HomeController> _logger;
        private IConfiguration _purecloudconfiguration;

        public Client client { get; set; }
        public ApiClient apiClient { get; set; }
        public WebChatApi webChatApi { get; set; }
        public Purecloudconfiguration pcconfiguration { get; set; }
        public CreateWebChatConversationResponse chatInfo { get; set; }
        public AuthTokenInfo accessTokenInfo { get; set; }

        public List<string> dataagent = new List<string>();

        PureCloudRegionHosts region = PureCloudRegionHosts.us_east_1;
        //PureCloudRegionHosts region = PureCloudRegionHosts.eu_west_1;


        [HttpPost]
        [Route("Chat/Index")]
        [IgnoreAntiforgeryToken(Order = 1001)]
        public async Task<IActionResult> IndexAsync([FromServices] IConfiguration purecloudconfiguration)
        {
            try
            {
                var fullname = Request.Form["name"].ToString().Split(' ');
                List<string> skills;
                string queue = Request.Form["queuename"];
                pcconfiguration = new Purecloudconfiguration() { integrations = new integrations() };

                apiClient = new ApiClient();
                chatInfo = new CreateWebChatConversationResponse();

                _purecloudconfiguration = purecloudconfiguration;
                _purecloudconfiguration.GetSection("integrations").Bind(pcconfiguration.integrations);

                string[] _skills = pcconfiguration.integrations.queue[queue].skills;
                skills = new List<string>(_skills);

                client = new Client()
                {
                    firstname = fullname[0],
                    lastname = (fullname.Length > 1) ? fullname[1] : "",
                    customerid = Request.Form["customerid"],
                    queuename = pcconfiguration.integrations.queue[queue].name
                };

                apiClient.setBasePath(region);

                accessTokenInfo = new AuthTokenInfo();
                accessTokenInfo = apiClient.PostToken(pcconfiguration.integrations.credentials.client_id,pcconfiguration.integrations.credentials.client_secret);
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
                        TargetAddress = client.queuename,
                        //Skills = skills,
                        Priority = 5

                    },
                    MemberInfo = new GuestMemberInfo()
                    {
                        DisplayName = client.firstname + " " + client.lastname,
                        CustomFields = new Dictionary<string, string>()
                    {
                        { "customField1Label", "Account"},
                        { "customField1", client.customerid.ToString()},
                        { "customField2Label", "IP Address"},
                        { "customField2", GetUserIP()},
                        { "customField3Label", "Agent"},
                        { "customField3", Request.Headers["User-Agent"].ToString()}

                    },
                        AvatarImageUrl = ""
                    }
                };

                webChatApi = new WebChatApi();
                chatInfo = await webChatApi.PostWebchatGuestConversationsAsync(chatbody);

                ViewBag.chatinformation = chatInfo;
                ViewBag.chatbody = chatbody;
                ViewBag.client = client;
                ViewBag.jwt = chatInfo.Jwt;
                ViewBag.token = accessTokenInfo.AccessToken;
                ViewBag.displayqueue = queue;

                string _lastrowindex = InsertChatSession(pcconfiguration.integrations.others.table,
                    chatInfo.Id,
                    chatInfo.Member.Id,
                    accessTokenInfo.AccessToken,
                    chatInfo.Jwt);

                ViewBag.newIndex = _lastrowindex;
                ViewBag.Agentname = "";
                ViewBag.tableid = pcconfiguration.integrations.others.table;

                return View();
            }
            catch (ApiException ex)
            {
                Console.WriteLine("Error in Index " + ex.Message + " | " + ex.InnerException);
                return View();
            }

        }

        [Route("Chat/LogOut")]
        public IActionResult LogOut()
        {
            return RedirectToAction("Index", "home");
        }

        // OK
        [HttpPost]
        [Route("Chat/SendMessage")]
        public async Task<JsonResult> SendMessageAsync(string messagetosend, string chatInfoId, string memberId, string token)
        {
            try
            {
                webChatApi = new WebChatApi();
                webChatApi.Configuration.DefaultHeader.Clear();
                webChatApi.Configuration.AddDefaultHeader("Content-Type", "application/json");
                webChatApi.Configuration.AddDefaultHeader("Authorization", "bearer " + token);

                CreateWebChatMessageRequest messageRequest = new CreateWebChatMessageRequest()
                {
                    BodyType = CreateWebChatMessageRequest.BodyTypeEnum.Standard,
                    Body = messagetosend
                };
                PureCloudPlatform.Client.V2.Model.WebChatMessage webChatMessage = await webChatApi.PostWebchatGuestConversationMemberMessagesAsync(chatInfoId, memberId, messageRequest);
                string result = webChatMessage.ToJson();
                JObject _result = JObject.Parse(result);
                var _json = JsonConvert.SerializeObject(_result);
                return Json(_json);
            }
            catch (ApiException ex)
            {
                PureCloudPlatform.Client.V2.Model.WebChatMessage webChatMessage = new WebChatMessage()
                {
                    BodyType = WebChatMessage.BodyTypeEnum.Notice,
                    Body = "There was an error sending the message " + messagetosend + "."
                };
                Console.WriteLine("Error in SendMessage " + ex.Message + " | " + ex.InnerException);
                string result = webChatMessage.ToJson();
                JObject _result = JObject.Parse(result);
                var _json = JsonConvert.SerializeObject(_result);
                return Json(_json);
            }
        }

        //OK
        [HttpPost]
        [Route("Chat/GetAgentData")]
        public async Task<JsonResult> GetAgentDataAsync(string chatInfoId, string agentId, string token)
        {
            try
            {
                dataagent.Clear();
                webChatApi = new WebChatApi();
                webChatApi.Configuration.DefaultHeader.Clear();
                webChatApi.Configuration.AddDefaultHeader("Content-Type", "application/json");
                webChatApi.Configuration.AddDefaultHeader("Authorization", "bearer " + token);

                PureCloudPlatform.Client.V2.Model.WebChatMemberInfo webChatMemberInfo = await webChatApi.GetWebchatGuestConversationMemberAsync(chatInfoId, agentId);
                if (webChatMemberInfo.Role == WebChatMemberInfo.RoleEnum.Agent)
                {
                    if (webChatMemberInfo.DisplayName != "")
                    {
                        dataagent.Add(webChatMemberInfo.DisplayName);
                    }
                    else
                    {
                        dataagent.Add("5Dimes");
                    }
                    dataagent.Add(webChatMemberInfo.AvatarImageUrl);
                    dataagent.Add(webChatMemberInfo.Id);
                }
                var _json = JsonConvert.SerializeObject(dataagent);
                return Json(_json);
            }
            catch (ApiException ex)
            {
                Console.WriteLine("Error in GetAgentData " + ex.Message + " | " + ex.InnerException);
                dataagent.Add("5Dimes");
                var _json = JsonConvert.SerializeObject(dataagent);
                return Json(_json);
            }
        }

        // OK
        [HttpPost]
        [Route("Chat/SendTyping")]
        public async Task<JsonResult> SendTypingAsync(string chatInfoId, string memberId, string token)
        {
            try
            {
                webChatApi = new WebChatApi();
                webChatApi.Configuration.DefaultHeader.Clear();
                webChatApi.Configuration.AddDefaultHeader("Content-Type", "application/json");
                webChatApi.Configuration.AddDefaultHeader("Authorization", "bearer " + token);

                PureCloudPlatform.Client.V2.Model.WebChatTyping webChatTyping = await webChatApi.PostWebchatGuestConversationMemberTypingAsync(chatInfoId, memberId);
                string result = webChatTyping.ToJson();
                JObject _result = JObject.Parse(result);
                var _json = JsonConvert.SerializeObject(_result);
                return Json(_json);
            }
            catch (ApiException ex)
            {
                Console.WriteLine("Error in SendTyping " + ex.Message + " | " + ex.InnerException);
                string result = @"{ ""SendTyping Result"":""FAIL"" }";
                JObject _result = JObject.Parse(result);
                var _json = JsonConvert.SerializeObject(_result);
                return Json(_json);
            }
        }

        [HttpPost]
        [Route("Chat/GetConversationData")]
        public async Task<JsonResult> GetConversationDataAsync(string chatInfoId, string token)
        {
            try
            {
                PureCloudPlatform.Client.V2.Api.WebChatApi webChatApi = new WebChatApi();
                webChatApi.Configuration.DefaultHeader.Clear();
                webChatApi.Configuration.AddDefaultHeader("Content-Type", "application/json");
                webChatApi.Configuration.AddDefaultHeader("Authorization", "bearer " + token);

                WebChatMemberInfoEntityList participants = await webChatApi.GetWebchatGuestConversationMembersAsync(chatInfoId, 100, 1, true);
                Console.WriteLine("Number of Participants " + participants.Total.Value.ToString());

                string result = participants.ToJson();
                JObject _result = JObject.Parse(result);
                var _json = JsonConvert.SerializeObject(_result);
                return Json(_json);
            }
            catch (ApiException ex)
            {
                Console.WriteLine("Error in GetConversationData " + ex.Message + " | " + ex.InnerException);
                string result = @"{ ""GetConversationData Result"":""FAIL"" }";
                JObject _result = JObject.Parse(result);
                var _json = JsonConvert.SerializeObject(_result);
                return Json(_json);
            }
        }

        // Section to CRUD data to the table.
        // OK
        private string InsertChatSession(string id, string chatInfoId, string MemberId, string token, string jwt)
        {
            try
            {
                PureCloudPlatform.Client.V2.Api.ArchitectApi architectApi = new ArchitectApi();
                architectApi.Configuration.DefaultHeader.Clear();
                architectApi.Configuration.AddDefaultHeader("Content-Type", "application/json");
                architectApi.Configuration.AddDefaultHeader("Authorization", "bearer " + Configuration.Default.AccessToken);

                DataTableRowEntityListing dataTableRowEntityListing = new DataTableRowEntityListing();
                dataTableRowEntityListing = architectApi.GetFlowsDatatableRows(id, 1, 100, true);

                string result = dataTableRowEntityListing.ToJson();
                JObject _result = JObject.Parse(result);
                JArray rows = (JArray)_result["entities"];
                string _lastrowindex = (string)rows.Last["key"];
                long lastrowindex = Convert.ToInt64(_lastrowindex);
                long newindex = lastrowindex + 1;

                var _json = JsonConvert.SerializeObject(_result);

                var content = new Dictionary<string, string>() { };
                content = new Dictionary<string, string>()
                {
                    { "KEY", newindex.ToString() },
                    { "jwt", jwt },
                    { "conversationid", chatInfoId },
                    { "agentid", MemberId },
                    { "date", DateTime.Now.ToString("MM/dd/yyyy") },
                    { "time", DateTime.Now.ToString("HH:mm:ss") }
                };

                var test = architectApi.PostFlowsDatatableRowsAsync(id, content);

                return newindex.ToString();
            }
            catch (ApiException ex)
            {
                Console.WriteLine("Error in InsertChatSession " + ex.Message + " | " + ex.InnerException);
                return null;
            }
        }

        // OK
        [HttpPost]
        [Route("Chat/UpdateChatSession")]
        public async Task<JsonResult> UpdateChatSessionAsync(string id, string rowindex)
        {
            try
            {
                PureCloudPlatform.Client.V2.Api.ArchitectApi architectApi = new ArchitectApi();
                architectApi.Configuration.DefaultHeader.Clear();
                architectApi.Configuration.AddDefaultHeader("Content-Type", "application/json");
                architectApi.Configuration.AddDefaultHeader("Authorization", "bearer " + Configuration.Default.AccessToken);

                DataTableRowEntityListing dataTableRowEntityListing = new DataTableRowEntityListing();
                await architectApi.DeleteFlowsDatatableRowAsync(id,rowindex);

                string result = @"{ ""UpdateChatSession Result"":""OKA"" }";
                JObject _result = JObject.Parse(result);
                var _json = JsonConvert.SerializeObject(_result);
                return Json(_json);
            }
            catch (ApiException ex)
            {
                Console.WriteLine("Error in UpdateChatSession " + ex.Message + " | " + ex.InnerException);
                string result = @"{ ""UpdateChatSession Result"":""FAIL"" }";
                JObject _result = JObject.Parse(result);
                var _json = JsonConvert.SerializeObject(_result);
                return Json(_json);
            }
        }

        // OK
        [HttpPost]
        [Route("Chat/EndSession")]
        public async Task<JsonResult> EndSessionAsync(string chatInfoId, string MemberId, string jwt)
        {
            try
            {
                PureCloudPlatform.Client.V2.Api.WebChatApi webChatApi = new WebChatApi();
                webChatApi.Configuration.DefaultHeader.Clear();
                webChatApi.Configuration.AddDefaultHeader("Content-Type", "application/json");
                webChatApi.Configuration.AddDefaultHeader("Authorization", "bearer " + jwt);

                await webChatApi.DeleteWebchatGuestConversationMemberAsync(chatInfoId, MemberId);

                string result = @"{ ""EndSession Result"":""OKA"" }";
                JObject _result = JObject.Parse(result);
                var _json = JsonConvert.SerializeObject(_result);
                return Json(_json);
            }
            catch (ApiException ex)
            {
                Console.WriteLine("Error in EndSession " + ex.Message + " | " + ex.InnerException);
                string result = @"{ ""EndSession Result"":""FAIL"" }";
                JObject _result = JObject.Parse(result);
                var _json = JsonConvert.SerializeObject(_result);
                return Json(_json);
            }
        }

        //OK
        public string GetUserIP()
        {
            string ipList;
            try
            {
                ipList = HttpContext.Request.Headers["X-FORWARDED-FOR"];
            }
            catch (Exception)
            {
                ipList = "Not Found";
            }
            return ipList; ;
        }


    }
}
