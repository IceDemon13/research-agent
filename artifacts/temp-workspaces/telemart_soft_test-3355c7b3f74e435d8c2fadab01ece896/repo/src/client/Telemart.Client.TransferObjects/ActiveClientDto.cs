using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ActiveClientDto
    {
        [JsonProperty("employee_name")]
        public string EmployeeName { get; set; }

        [JsonProperty("logged_in")]
        public DateTime LoggedIn { get; set; }

        [JsonProperty("user_agent_version")]
        public string UserAgentVersion { get; set; }
    }
}