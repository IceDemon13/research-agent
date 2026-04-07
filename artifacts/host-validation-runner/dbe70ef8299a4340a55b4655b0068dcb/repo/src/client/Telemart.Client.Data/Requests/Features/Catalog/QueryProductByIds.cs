using System.Collections.Generic;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Catalog
{
    public sealed class QueryProductByIds : CallActionWithBodyRequestBase<List<ProductDto>, QueryProductByIdsDto>
    {
        public QueryProductByIds(
            int contractorId,
            int[] ids,
            bool queryGifts = false,
            bool queryAdditionalServices = false,
            bool priceIn = false,
            bool priceInIncludeReserve = false,
            bool includePriceJson = false,
            bool additionalServiceProvideProducts = false,
            int[] cartProductIds = null,
            bool? conditionGifts = null)
            : this(new QueryProductByIdsDto(
                ids,
                contractorId,
                queryGifts,
                queryAdditionalServices,
                priceIn,
                priceInIncludeReserve,
                includePriceJson,
                additionalServiceProvideProducts: additionalServiceProvideProducts,
                cartProductIds: cartProductIds,
                conditionGifts: conditionGifts))
        {
        }

        public QueryProductByIds(QueryProductByIdsDto dto)
            : base(dto, "products", "search")
        {
        }
    }
}