using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Tools
{
    public sealed class ChangeLegalEntity : CallActionWithBodyRequestResultBase<object, ChangeLegalEntityDto>
    {
        public ChangeLegalEntity(ChangeLegalEntityDto dto)
            : base(dto, ApiResources.TechSupport, "change_legal_entity")
        {
        }
    }
}