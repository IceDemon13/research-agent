using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.ParserSearchTemplate;

namespace Telemart.Client.Data.Requests.Features.ParserSearchTemplate
{
    public sealed class SaveProductSearchTemplates : CallActionWithBodyRequestResultBase<object, ProductSearchtemplateSaveDto>
    {
        public SaveProductSearchTemplates(ProductSearchtemplateSaveDto saveDto)
            : base(saveDto, ApiResources.ParserSearchTemplates, "save_products")
        {
        }
    }
}