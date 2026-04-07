using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.AssemblyService
{
    public class AssemblyServiceProductSaveDto
    {
        public AssemblyServiceProductSaveDto(int id, int scannedQuantity, IReadOnlyCollection<string> serialNumbers)
        {
            Id = id;
            ScannedQuantity = scannedQuantity;
            SerialNumbers = serialNumbers;
        }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("scanned_quantity")]
        public int ScannedQuantity { get; set; }

        [JsonProperty("serial_numbers")]
        public IReadOnlyCollection<string> SerialNumbers { get; set; }
    }
}