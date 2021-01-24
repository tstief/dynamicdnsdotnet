using System.Collections.Generic;

namespace dynamicdnsdotnet
{
    public class Configuration
    {
        public int Interval{ get; set; }
        public string IPProviderURL{ get; set; }
        public bool EnableHeartBeat { get; set; }
        public List<Host> Hosts { get; set; }
        public bool EnableEmail { get; set; }
        public List<string> EmailAddresses { get; set; }
        public string MailApiKey { get; set; }
        public string MailDomain { get; set; }
        public string MailProviderUri { get; set; }
        public int WebRequestTimeOut { get; set; }
    }
}