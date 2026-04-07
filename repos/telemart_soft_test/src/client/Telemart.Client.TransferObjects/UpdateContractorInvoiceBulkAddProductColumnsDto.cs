using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class UpdateContractorInvoiceBulkAddProductColumnsDto
    {
        public UpdateContractorInvoiceBulkAddProductColumnsDto(List<string> columns)
        {
            Columns = columns;
        }

        [JsonProperty("columns")]
        public List<string> Columns { get; set; }
    }
}