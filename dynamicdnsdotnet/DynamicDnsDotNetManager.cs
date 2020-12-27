using System;
using System.Timers;
using System.IO;
using System.Threading;
using RestSharp;
using RestSharp.Authenticators;

namespace dynamicdnsdotnet
{
    public class DynamicDnsManagerDotNet
    {
        public DynamicDnsManagerDotNet(string workingDirectory, Configuration configuration)
        {
            WorkingDirectory = workingDirectory;
            Configuration = configuration;
            Timer = new System.Timers.Timer();
            Timer.Interval = configuration.Interval * 1000;

            // Hook up the Elapsed event for the timer. 
            Timer.Elapsed += OnTimedEvent;

            // Have the timer fire repeated events (true is the default)
            Timer.AutoReset = true;
        }

        public void Start()
        {
            Started = false;
            Console.WriteLine("Starting Dynamic DNS");
            SendSimpleMessage("Starting Dynamic DNS");
            CheckIpAddress();
            Timer.Start();
            Started = true;
            while (true){ Thread.Sleep(1000); }
        }

        private string GetCurrentIPFromProvider()
        {
            var ip = string.Empty;           
            
            var client = new RestClient();
            client.BaseUrl = new Uri(Configuration.IPProviderURL);
            var request = new RestRequest();
            request.Method = Method.GET;
            var response = client.Execute(request);
            if(response.IsSuccessful)
            {
                ip = response.Content.TrimEnd('\n');
            }
            else
            {
                Console.WriteLine($"Unable to obtain current external IP address from provider: {response.ErrorMessage}");
            }

            return ip;
        }

        private void WriteCurrentIPToFile(string currentIp)
        {
            var currentIpFilePath = Path.Combine(WorkingDirectory, "currentIp.txt");
            string[] lines = { currentIp };
            File.WriteAllLines(currentIpFilePath, lines);
        }

        private string ReadCurrentIPFromFile()
        {
            var currentIpFilePath = Path.Combine(WorkingDirectory, "currentIp.txt");
            if(File.Exists(currentIpFilePath))
            {
                var lines = File.ReadAllLines(currentIpFilePath);
                if(lines.Length > 0)
                {
                    return lines[0];
                }
            }
            return string.Empty;
        }

        private void UpdateHost(string ipAddress, Host host)
        {
            var client = new RestClient();
            client.BaseUrl = new Uri($"https://domains.google.com/nic/update?hostname={host.Name}&myip={ipAddress}");
            client.Authenticator =
                new HttpBasicAuthenticator(host.Username, host.Password);
            var request = new RestRequest();
            request.Method = Method.POST;
            var response = client.Execute(request);

            string logMessage;
            if(response.IsSuccessful)
            {
                logMessage = $"Updating host={host.Name}, ip={ipAddress}, server response={response.Content}";
            }
            else
            {
                logMessage = $"Error updating host={host.Name}, ip={ipAddress}, error message={response.ErrorMessage}";
            }
            
            Console.WriteLine(logMessage);
            SendSimpleMessage(logMessage);
        }

        private void CheckIpAddress()
        {
            try
            {
                string currentIpFromFile = ReadCurrentIPFromFile();
                string ipFromProvider = GetCurrentIPFromProvider();

                if(!Started || Configuration.EnableHeartBeat)
                {
                    Console.WriteLine($"Checking for IP changes old={currentIpFromFile}, new={ipFromProvider}");
                }

                if(currentIpFromFile != ipFromProvider)
                {
                    foreach(var host in Configuration.Hosts)
                    {
                        UpdateHost(ipFromProvider, host);
                    }
                    WriteCurrentIPToFile(ipFromProvider);
                }
            }
            catch(Exception ex)
            {
                var exceptionMessage = $"Exception: {ex.Message}";
                Console.WriteLine(exceptionMessage);
                SendSimpleMessage(exceptionMessage);
            }
        }

        public IRestResponse SendSimpleMessage(string message)
        {
            if (Configuration.EnableEmail)
            {
                RestClient client = new RestClient();
                client.BaseUrl = new Uri(Configuration.MailProviderUri);
                client.Authenticator =
                    new HttpBasicAuthenticator("api",
                                               Configuration.MailApiKey);
                RestRequest request = new RestRequest();
                request.AddParameter("domain", Configuration.MailDomain, ParameterType.UrlSegment);
                request.Resource = "{domain}/messages";
                request.AddParameter("from", $"noreply@{Configuration.MailDomain}");
                Configuration.EmailAddresses.ForEach(address => request.AddParameter("to", address));
                request.AddParameter("subject", "Dynamic DNS Dot Net Message");
                request.AddParameter("text", message);
                request.Method = Method.POST;
                return client.Execute(request);
            }
            return null;
        }

        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            CheckIpAddress();
        }

        private System.Timers.Timer Timer
        {
            get; set;
        }

        private Configuration Configuration
        {
            get; set;
        }

        private string WorkingDirectory
        {
            get; set;
        }

        private bool Started
        {
            get; set;
        }
    }
}