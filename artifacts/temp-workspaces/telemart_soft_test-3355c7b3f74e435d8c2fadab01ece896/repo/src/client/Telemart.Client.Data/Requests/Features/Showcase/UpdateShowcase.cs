using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Showcase;

namespace Telemart.Client.Data.Requests.Features.Showcase
{
    public class UpdateShowcase : UpdateEntityResultRequestBase<ShowcaseDto, UpdateShowcase.ShowcaseSaveDto>
    {
        public UpdateShowcase(int id, int capacity, bool active)
            : base(new ShowcaseSaveDto(id, capacity, active), ApiResources.Showcases, id)
        {
        }

        public class ShowcaseSaveDto
        {
            public ShowcaseSaveDto(int id, int capacity, bool active)
            {
                Id = id;
                Capacity = capacity;
                Active = active;
            }

            [JsonProperty("id")]
            public int Id { get; set; }

            [JsonProperty("capacity")]
            public int Capacity { get; set; }

            [JsonProperty("active")]
            public bool Active { get; set; }
        }
    }
}
