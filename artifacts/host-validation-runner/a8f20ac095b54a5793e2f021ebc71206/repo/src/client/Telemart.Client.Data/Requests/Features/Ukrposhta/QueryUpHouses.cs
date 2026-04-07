using System.Collections.Generic;
using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.Ukrposhta;

namespace Telemart.Client.Data.Requests.Features.Ukrposhta
{
    public sealed class QueryUpHouses : CallActionWithBodyRequestResultBase<List<UpHouseDto>, QueryUpHouses.UpStreet>
    {
        public QueryUpHouses(int upStreetId)
            : base(new UpStreet(upStreetId), ApiResources.Ukrposhta, "up_houses")
        {
        }

        public class UpStreet
        {
            public UpStreet(int upStreetId)
            {
                UpStreetId = upStreetId;
            }

            [JsonProperty("up_street_id")]
            public int UpStreetId { get; set; }
        }
    }
}