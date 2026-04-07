using System.Collections.Generic;
using System.Net;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Products
{
    public class SaveProductEquipments : CreateEntityRequestBase<object, IReadOnlyCollection<ProductEquipmentDto>>
    {
        public SaveProductEquipments(IReadOnlyCollection<ProductEquipmentDto> dto)
            : base(dto, ApiResources.Products, "equipments")
        {
            SuccessStatusCode = HttpStatusCode.NoContent;
        }
    }
}