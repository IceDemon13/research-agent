using System;
using System.Net;
using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Carry
{
    public sealed class CreateScanSheetForEntitiesRequest : CreateEntityResultRequestBase<ScanSheetCreateResponse[], ScanSheetCreateForEntitiesRequest>
    {
        public CreateScanSheetForEntitiesRequest(int entityTypeId, int[] entityIds, ScanSheetPrefix type)
            : base(new ScanSheetCreateForEntitiesRequest { EntityIds = entityIds }, ApiResources.ScanSheets, type, "entity_type", entityTypeId)
        {
            SuccessStatusCode = HttpStatusCode.OK;

            DefaultTimeout = TimeSpan.FromMinutes(5);
        }
    }

    public class ScanSheetCreateForEntitiesRequest
    {
        [JsonProperty("entity_ids")]
        public int[] EntityIds { get; set; }
    }
}