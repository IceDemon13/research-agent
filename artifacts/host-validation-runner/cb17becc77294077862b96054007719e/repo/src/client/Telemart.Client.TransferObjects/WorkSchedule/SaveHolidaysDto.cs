using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.WorkSchedule
{
    public sealed class SaveHolidaysDto
    {
        public SaveHolidaysDto(IReadOnlyCollection<SaveHolidayDto> values)
        {
            Values = values;
        }

        [JsonProperty("holidays")]
        public IReadOnlyCollection<SaveHolidayDto> Values { get; }
    }
}