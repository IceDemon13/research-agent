using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.City
{
    public sealed class CityCarryDto
    {
        public CityCarryDto()
        {
        }

        public CityCarryDto(int id, int carryId, bool availOnWeb, int createdBy)
        {
            Id = id;
            CarryId = carryId;
            AvailOnWeb = availOnWeb;
            CreatedBy = createdBy;
        }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("carry_id")]
        public int CarryId { get; set; }

        [JsonProperty("created_by")]
        public int CreatedBy { get; set; }

        [JsonProperty("avail_on_web")]
        public bool AvailOnWeb { get; set; }
    }
}