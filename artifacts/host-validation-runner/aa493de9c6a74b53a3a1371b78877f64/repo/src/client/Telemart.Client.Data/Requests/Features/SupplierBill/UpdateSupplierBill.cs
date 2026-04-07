using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.SupplierBill;

namespace Telemart.Client.Data.Requests.Features.SupplierBill
{
    public class UpdateSupplierBill : UpdateEntityResultRequestBase<SupplierBillDto, SupplierBillSaveDto>
    {
        public UpdateSupplierBill(int id, SupplierBillSaveDto dto)
            : base(dto, ApiResources.SupplierBills, id)
        {
        }
    }
}
