using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Customer;

namespace Telemart.Client.Data.Requests.Features.Customer
{
    public class UpdateCustomer : UpdateEntityResultRequestBase<CustomerDto, CustomerSaveDto>
    {
        public UpdateCustomer(CustomerSaveDto dto)
            : base(dto, ApiResources.Customers, dto.Id)
        {
        }
    }
}
