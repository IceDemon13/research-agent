using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Contractor.Actions
{
    public class UpdateContractorInvoiceBulkAddProductColumns : CallEntityActionWithBodyRequestResultBase<ContractorDto, UpdateContractorInvoiceBulkAddProductColumnsDto>
    {
        public UpdateContractorInvoiceBulkAddProductColumns(int contractorId, UpdateContractorInvoiceBulkAddProductColumnsDto dto)
            : base(contractorId, dto, ApiResources.Contractors, "invoice_bulk_add_product_columns")
        {
        }
    }
}