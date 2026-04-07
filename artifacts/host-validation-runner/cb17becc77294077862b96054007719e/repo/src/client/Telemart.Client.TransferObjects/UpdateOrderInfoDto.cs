using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class UpdateOrderInfoDto
    {
        public UpdateOrderInfoDto(int id, DateTime? receiveTime, string comment)
        {
            Id = id;
            ReceiveTime = receiveTime;
            Comment = comment;
        }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("receive_time")]
        public DateTime? ReceiveTime { get; set; }

        [JsonProperty("comment")]
        public string Comment { get; set; }
    }
}