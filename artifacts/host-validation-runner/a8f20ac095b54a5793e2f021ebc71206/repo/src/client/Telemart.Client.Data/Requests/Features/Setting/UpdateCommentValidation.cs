using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Setting
{
    public sealed class UpdateCommentValidation : UpdateEntityRequestBase<object, UpdateCommentValidationDto>
    {
        public UpdateCommentValidation(UpdateCommentValidationDto dto)
            : base(dto, ApiResources.Settings, "comment_validation")
        {
        }
    }
}