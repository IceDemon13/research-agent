using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ExternalPayment
{
    public sealed class UpdateExternalPayment : UpdateEntityResultRequestBase<object, UpdateExternalPaymentDto>
    {
        public UpdateExternalPayment(int id, UpdateExternalPaymentDto dto)
            : base(dto, ApiResources.ExternalPayments, id)
        {
        }
    }
}