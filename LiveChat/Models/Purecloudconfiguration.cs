using System.Collections.Generic;

namespace LiveChat.Models
{
    public partial class Purecloudconfiguration
    {
        public integrations integrations { get; set; }
    }

    public partial class integrations
    {
        public Dictionary<string, queue> queue { get; set; }
        public credentials credentials { get; set; }
        public environment environment { get; set; }
        public uri uri { get; set; }
        public deployment deployment { get; set; }
        public others others { get; set; }
        public organization organization { get; set; }
    }

    public partial class credentials
    {
        public string client_id { get; set; }
        public string client_secret { get; set; }
    }

    public partial class deployment
    {
        public string id { get; set; }
    }

    public partial class environment
    {
        public string host { get; set; }
        public string login { get; set; }
        public string api { get; set; }
    }

    public partial class organization
    {
        public string id { get; set; }
        public string region { get; set; }
    }

    public partial class others
    {
        public string grant_type { get; set; }
        public string content_type { get; set; }
        public string language { get; set; }
        public string table { get; set; }
    }

    public partial class uri
    {
        public string token { get; set; }
    }

    public partial class queue
    {
        public string name { get; set; }
        public string[] skills { get; set; }
        public int index { get; set; }
    }
    
}
