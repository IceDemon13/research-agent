using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Bitrix
{
    public class BitrixTasksDto
    {
        [JsonProperty("new_tasks")]
        public List<BitrixTaskDto> NewTasks { get; set; }

        [JsonProperty("tasks")]
        public List<CategorizedBitrixTaskDto> Tasks { get; set; }
    }
}