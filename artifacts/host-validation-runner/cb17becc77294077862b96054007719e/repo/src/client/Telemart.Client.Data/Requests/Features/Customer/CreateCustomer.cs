using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Customer;

namespace Telemart.Client.Data.Requests.Features.Customer
{
    public sealed class CreateCustomer : CreateEntityResultRequestBase<CustomerDto, CustomerCreateDto>
    {
        public CreateCustomer(CustomerCreateDto dto)
            : base(dto, ApiResources.Customers)
        {
        }
    }
}
