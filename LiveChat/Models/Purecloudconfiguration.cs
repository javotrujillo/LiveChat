using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LiveChat.Models
{
    public class Purecloudconfiguration
    {
        public string client_id { get; set; }
        public string client_secret { get; set; }
        public string grant_type { get; set; }
        public string content_type { get; set; }
        public string url_host { get; set; }
        public string url_login { get; set; }
        public string url_api { get; set; }
        public string uri_token { get; set; }

        //Organization
        public string organizationid { get; set; }
        public string deploymentid { get; set; }
        public string region { get; set; }

    }
}