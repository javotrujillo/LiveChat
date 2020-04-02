using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LiveChat.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Logging;
using PureCloudPlatform.Client.V2.Client;
using PureCloudPlatform.Client.V2.Api;
using PureCloudPlatform.Client.V2.Model;
using Microsoft.Extensions.Configuration;
//using System.Net.WebSockets;
using Newtonsoft.Json;
using PureCloudPlatform.Client.V2.Extensions;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Web;

namespace LiveChat.Controllers
{
    public class ChatController : Microsoft.AspNetCore.Mvc.Controller
    {
        private readonly ILogger<HomeController> _logger;
        private IConfiguration _purecloudconfiguration;

        public Client client { get; set; }
        //public PureCloudPlatform.Client.V2.Client.Configuration configuration { get; set; }
        public ApiClient apiclient { get; set; }
        public WebChatApi webChatApi { get; set; }
        public Purecloudconfiguration pcconfiguration { get; set; }
        public CreateWebChatConversationResponse chatInfo { get; set; }
        public AuthTokenInfo accessTokenInfo { get; set; }

        public List<string> dataagent = new List<string>();

        PureCloudRegionHosts region = PureCloudRegionHosts.us_east_1;
        //PureCloudRegionHosts region = PureCloudRegionHosts.eu_west_1;


        [HttpPost]
        [Route("Chat/Index")]
        public async Task<IActionResult> IndexAsync([FromServices] IConfiguration purecloudconfiguration)
        {
            try
            {
                var fullname = Request.Form["name"].ToString().Split(' ');
                List<string> skills;
                string queue = Request.Form["queuename"];

                pcconfiguration = new Purecloudconfiguration() { integrations = new integrations() };
                webChatApi = new WebChatApi();
                apiclient = new ApiClient();
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

                //configuration = new Configuration();

                //configuration.ApiClient.setBasePath(region);

                Configuration.Default.ApiClient.setBasePath(region);

                accessTokenInfo = new AuthTokenInfo();
                //accessTokenInfo = configuration.ApiClient.PostToken(
                accessTokenInfo = Configuration.Default.ApiClient.PostToken(
                    pcconfiguration.integrations.credentials.client_id,
                    pcconfiguration.integrations.credentials.client_secret);
                accessTokenInfo.AccessToken = accessTokenInfo.AccessToken;
                accessTokenInfo.TokenType = accessTokenInfo.TokenType;

                //configuration.AccessToken = accessTokenInfo.AccessToken;
                Configuration.Default.ApiClient.Configuration.AccessToken = accessTokenInfo.AccessToken;

                CreateWebChatConversationRequest chatbody = new CreateWebChatConversationRequest()
                {
                    DeploymentId = pcconfiguration.integrations.deployment.id,
                    OrganizationId = pcconfiguration.integrations.organization.id,
                    RoutingTarget = new WebChatRoutingTarget()
                    {
                        Language = pcconfiguration.integrations.others.language,
                        TargetType = WebChatRoutingTarget.TargetTypeEnum.Queue,
                        TargetAddress = queue,
                        Skills = skills,
                        Priority = 5

                    },
                    MemberInfo = new GuestMemberInfo()
                    {
                        DisplayName = client.firstname + " " + client.lastname,
                        CustomFields = new Dictionary<string, string>()
                    {
                        { "phoneNumber", "" },
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

                chatInfo = await webChatApi.PostWebchatGuestConversationsAsync(chatbody);
                //chatInfo.Member.Role = WebChatMemberInfo.RoleEnum.Customer;
                //chatInfo.Member.State = WebChatMemberInfo.StateEnum.Connected;

                ViewBag.chatinformation = chatInfo;
                ViewBag.chatbody = chatbody;
                ViewBag.client = client;
                ViewBag.jwt = chatInfo.Jwt;
                ViewBag.token = accessTokenInfo.AccessToken;

                ViewBag.host = pcconfiguration.integrations.environment.host;
                ViewBag.api = pcconfiguration.integrations.environment.api;
                ViewBag.content_type = pcconfiguration.integrations.others.content_type;
                ViewBag.tableid = pcconfiguration.integrations.others.table;

                string _lastrowindex = InsertChatSession(pcconfiguration.integrations.others.table, chatInfo.Id, chatInfo.Member.Id, accessTokenInfo.AccessToken,
                    pcconfiguration.integrations.others.content_type, pcconfiguration.integrations.environment.api,
                    pcconfiguration.integrations.environment.host, chatInfo.Jwt
                    );

                ViewBag.newIndex = _lastrowindex;

                ViewBag.Agentname = "";
            }
            catch (ApiException ex)
            {
                Console.WriteLine(ex.InnerException + " | " + ex.Message);
            }

            return View();

        }

        [Route("Chat/LogOut")]
        public IActionResult LogOut()
        {
            return RedirectToAction("Index", "home");
        }

        // OK
        [HttpPost]
        [Route("Chat/SendMessage")]
        public async Task<JsonResult> SendMessageAsync(string messagetosend, string chatInfoId, string memberId, string token, [FromServices] IConfiguration purecloudconfiguration)
        {
            try
            {
                pcconfiguration = new Purecloudconfiguration() { integrations = new integrations() };
                _purecloudconfiguration = purecloudconfiguration;
                _purecloudconfiguration.GetSection("integrations").Bind(pcconfiguration.integrations);

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
                    Body = ex.Message
                };
                string result = webChatMessage.ToJson();
                JObject _result = JObject.Parse(result);
                var _json = JsonConvert.SerializeObject(_result);
                return Json(_json);
            }
        }

        //OK
        [HttpPost]
        [Route("Chat/GetAgentData")]
        public async Task<JsonResult> GetAgentDataAsync(string chatInfoId, string agentId, string token, [FromServices] IConfiguration purecloudconfiguration)
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
                    dataagent.Add(webChatMemberInfo.DisplayName);
                    dataagent.Add(webChatMemberInfo.AvatarImageUrl);
                    dataagent.Add(webChatMemberInfo.Id);
                }
                var _json = JsonConvert.SerializeObject(dataagent);
                return Json(_json);
            }
            catch (ApiException ex)
            {
                var _json = JsonConvert.SerializeObject(dataagent);
                return Json(_json);
            }
        }

        // OK
        [HttpPost]
        [Route("Chat/SendTyping")]
        public async Task<JsonResult> SendTypingAsync(string chatInfoId, string memberId, string token, [FromServices] IConfiguration purecloudconfiguration)
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
                PureCloudPlatform.Client.V2.Model.WebChatTyping webChatTyping = new WebChatTyping();
                string result = webChatTyping.ToJson();
                JObject _result = JObject.Parse(result);
                var _json = JsonConvert.SerializeObject(_result);
                return Json(_json);
            }
        }



        //TODO: Get the current status of the call
        [HttpPost]
        [Route("Chat/GetConversationData")]
        public async Task<JsonResult> GetConversationDataAsync(string chatInfoId, string token)
        {
            try
            {
                PureCloudPlatform.Client.V2.Api.AnalyticsApi analyticsApi = new AnalyticsApi();
                analyticsApi.Configuration.DefaultHeader.Clear();
                analyticsApi.Configuration.AddDefaultHeader("Content-Type", "application/json");
                analyticsApi.Configuration.AddDefaultHeader("Authorization", "bearer " + token);

                PureCloudPlatform.Client.V2.Model.AnalyticsConversationWithoutAttributes analyticsConversation = new AnalyticsConversationWithoutAttributes();
                analyticsConversation = await analyticsApi.GetAnalyticsConversationDetailsAsync(chatInfoId);

                string result = analyticsConversation.ToJson();
                JObject _result = JObject.Parse(result);
                var _json = JsonConvert.SerializeObject(_result);
                return Json(_json);
            }
            catch (ApiException ex)
            {
                return Json("");

            }
        }



        // Section to CRUD data to the table.
        // OK
        private string InsertChatSession(string id, string chatInfoId, string MemberId, string token, string content_type, string api, string host, string jwt)
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
                ex.Message.ToString();
                return null;
            }
        }

        // OK
        public JsonResult UpdateChatSession(string id, string rowindex, string token, string content_type, string api, string host)
        {
            try
            {
                HttpClient client = new HttpClient();
                var content = new Dictionary<string, string>() { };

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);
                client.BaseAddress = new Uri(api + host);
                client.DefaultRequestHeaders
                    .Accept
                    .Add(new MediaTypeWithQualityHeaderValue(content_type));
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, "/api/v2/flows/datatables/" + id + "/rows/" + rowindex);
                HttpResponseMessage result = client.SendAsync(request).Result;
                result.EnsureSuccessStatusCode();
                HttpContent _content = result.Content;
                string _jsonContent = _content.ReadAsStringAsync().Result;
                var json = JsonConvert.SerializeObject(_jsonContent);
                return null;
            }
            catch (Exception ex)
            {
                //Models.Client client = new Client() { Phone = phone.ToString(), Name = ex.InnerException.Message, Lastname = ex.Message };
                //return client;
                ex.Message.ToString();
                return null;
            }
        }

        //TODO: Improve this 
        // OK
        public JsonResult EndSession(string chatInfoId, string MemberId, string jwt, string content_type, string api, string host)
        {
            //var result = await client.DeleteAsync("/api/v2/webchat/guest/conversations/" + chatInfoId + "/members/" + MemberId);

            try
            {
                HttpClient client = new HttpClient();

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", jwt);
                client.BaseAddress = new Uri(api + host);
                client.DefaultRequestHeaders
                    .Accept
                    .Add(new MediaTypeWithQualityHeaderValue(content_type));
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, "/api/v2/webchat/guest/conversations/" + chatInfoId + "/members/" + MemberId);
                HttpResponseMessage result = client.SendAsync(request).Result;
                result.EnsureSuccessStatusCode();
                HttpContent _content = result.Content;
                string _jsonContent = _content.ReadAsStringAsync().Result;

                var _json = JsonConvert.SerializeObject(_jsonContent);
                return Json(_json);
            }
            catch (Exception ex)
            {
                ex.Message.ToString();
                return null;
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
