using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telemart.Client.ViewModels.Nomenclature;

namespace Telemart.Client.ViewModels.Store.Order
{
    public class OrderProductBulkAddParameter
    {
        public OrderProductBulkAddParameter(NomenclatureViewPriceContext priceContext, int contractorId, bool queryGifts, bool queryAdditionalServices, int[] cartProductIds)
        {
            PriceContext = priceContext;
            ContractorId = contractorId;
            QueryGifts = queryGifts;
            QueryAdditionalServices = queryAdditionalServices;
            CartProductIds = cartProductIds;
        }

        public NomenclatureViewPriceContext PriceContext { get; }

        public int ContractorId { get; }

        public bool QueryGifts { get; }

        public bool QueryAdditionalServices { get; }

        public int[] CartProductIds { get; }
    }
}