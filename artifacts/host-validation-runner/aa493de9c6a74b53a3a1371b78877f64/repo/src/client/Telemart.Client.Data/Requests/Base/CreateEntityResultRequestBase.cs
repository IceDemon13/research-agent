using Telemart.Common.ErrorHandling;

namespace Telemart.Client.Data.Requests.Base
{
    public abstract class CreateEntityResultRequestBase<T, TCreateDto> : CreateEntityRequestBase<Result<T>, TCreateDto>
    {
        protected CreateEntityResultRequestBase(TCreateDto dto, params object[] pathParameters)
            : base(dto, pathParameters)
        {
        }
    }
}