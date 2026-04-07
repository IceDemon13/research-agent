namespace Telemart.Client.ViewModels.Store.Order.CreateCompleted
{
    public class CreateCompletedOrderParameter
    {
        public CreateCompletedOrderParameter(int? contractorTemplateId)
        {
            ContractorTemplateId = contractorTemplateId;
        }

        public int? ContractorTemplateId { get; }
    }
}
