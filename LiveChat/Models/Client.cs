using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LiveChat.Models
{
    public class Client
    {
        public string firstname { get; set; }
        public string lastname { get; set; }
        public string customerid { get; set; }
        public string queuename { get; set; }
        public string addressstreet { get; set; }
        public string addresscity { get; set; }
        public string addresspostalcode { get; set; }
        public string addressState { get; set; }
        public string phonenumber { get; set; }
        public string customfield1label { get; set; }
        public string customfield1 { get; set; }
        public string customfield2label { get; set; }
        public string customfield2 { get; set; }
        public string customfield3label { get; set; }
        public string customfield3 { get; set; }
        public string email { get; set; }
    }
}
