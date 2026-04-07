using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.SupplierBill;

namespace Telemart.Client.Data.Requests.Features.SupplierBill
{
    public class CreateSupplierBillDocument : CreateEntityResultRequestBase<SupplierBillDocumentDto, SupplierBillDocumentDto>
    {
        public CreateSupplierBillDocument(SupplierBillDocumentDto dto)
            : base(dto, ApiResources.SupplierBills, dto.SupplierBillId, "documents")
        {
        }
    }
}