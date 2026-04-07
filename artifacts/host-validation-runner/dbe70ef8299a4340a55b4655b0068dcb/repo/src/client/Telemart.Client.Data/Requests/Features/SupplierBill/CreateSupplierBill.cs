using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.SupplierBill;

namespace Telemart.Client.Data.Requests.Features.SupplierBill
{
    public class CreateSupplierBill : CreateEntityResultRequestBase<SupplierBillDto, SupplierBillCreateDto>
    {
        public CreateSupplierBill(SupplierBillCreateDto dto)
            : base(dto, ApiResources.SupplierBills)
        {
        }
    }
}
