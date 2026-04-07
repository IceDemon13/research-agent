using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Customer;

namespace Telemart.Client.Common.Messages
{
    public sealed class OrderCreateViewMessage
    {
        public OrderCreateViewMessage(ContractorTemplateDto contractorTemplate = null)
        {
            ContractorTemplate = contractorTemplate;
        }

        public OrderCreateViewMessage(CustomerDto customer)
        {
            Customer = customer;
        }

        public ContractorTemplateDto ContractorTemplate { get; }

        public CustomerDto Customer { get; }
    }
}
