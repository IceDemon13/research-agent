using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.SupplierBill;

namespace Telemart.Client.Data.Requests.Features.SupplierBill.Actions
{
    public sealed class RecognizeSupplierBill : CallEntityActionWithBodyRequestResultBase<SupplierBillRecognizeResultDto[], SupplierBillRecognizeDto[]>
    {
        public RecognizeSupplierBill(int id, SupplierBillRecognizeDto[] dto)
            : base(id, dto, ApiResources.SupplierBills, "recognize")
        {
        }
    }
}
