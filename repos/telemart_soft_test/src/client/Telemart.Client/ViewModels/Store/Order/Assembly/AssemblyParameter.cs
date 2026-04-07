using System.Collections.Generic;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.ViewModels.Store.Order.Assembly
{
    public class AssemblyParameter
    {
        public AssemblyParameter(int contractorId, bool queryGifts, bool queryAdditionalServices, OrderFolderDto orderFolder, IReadOnlyCollection<AssemblyParameterProduct> products = null, bool anyAssemblyServices = false)
        {
            ContractorId = contractorId;
            QueryGifts = queryGifts;
            OrderFolder = orderFolder;
            Products = products;
            AnyAssemblyServices = anyAssemblyServices;
            QueryAdditionalServices = queryAdditionalServices;
        }

        public int ContractorId { get; }

        public bool QueryGifts { get; }

        public bool QueryAdditionalServices { get; }

        public OrderFolderDto OrderFolder { get; }

        public bool AnyAssemblyServices { get; }

        public IReadOnlyCollection<AssemblyParameterProduct> Products { get; }
    }
}
