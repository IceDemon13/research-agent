using System.Collections.Generic;
using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.Novaposhta;

namespace Telemart.Client.Data.Requests.Features.Novaposhta.Actions
{
    public sealed class TrackNpDocument : CallEntityActionWithBodyRequestResultBase<List<NewPostDocumentDto>, TrackNpDocument.TrackNpDto>
    {
        public TrackNpDocument(string trackNumber, string phone)
        : base(trackNumber, new TrackNpDto(trackNumber, phone), ApiResources.NovaposhtaDocuments, "track")
        {
        }

        public class TrackNpDto
        {
            public TrackNpDto(string trackNumber, string phone)
            {
                TrackNumber = trackNumber;
                Phone = phone;
            }

            [JsonProperty("track_number")]
            public string TrackNumber { get; }

            [JsonProperty("phone")]
            public string Phone { get; }
        }
    }
}