using System;
using System.Net;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Carry
{
    public class CreateScanSheetRequest : CreateEntityResultRequestBase<ScanSheetCreateResponse[], ScanSheetCreateRequest>
    {
        public CreateScanSheetRequest(int[] orderIds, ScanSheetPrefix type, bool? completeCourierCall = null)
            : base(new ScanSheetCreateRequest { OrderIds = orderIds, CompleteCourierCallApplication = completeCourierCall }, ApiResources.ScanSheets, type)
        {
            SuccessStatusCode = HttpStatusCode.OK;

            DefaultTimeout = TimeSpan.FromMinutes(5);
        }
    }
}