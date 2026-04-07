using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.MeestExpress
{
    public class MeTrackDto
    {
        [JsonProperty("parcelNumber")]
        public string ParcelNumber { get; set; }

        [JsonProperty("barCode")]
        public string Barcode { get; set; }

        [JsonProperty("eventDateTime")]
        public DateTime TimeStamp { get; set; }

        [JsonProperty("eventCode")]
        public string EventCode { get; set; }

        [JsonProperty("eventDescr")]
        public MeTrackDescriptionDto Description { get; set; }

        [JsonProperty("eventDetailDescr")]
        public MeTrackDescriptionDetailDto DescriptionDetail { get; set; }
    }
}