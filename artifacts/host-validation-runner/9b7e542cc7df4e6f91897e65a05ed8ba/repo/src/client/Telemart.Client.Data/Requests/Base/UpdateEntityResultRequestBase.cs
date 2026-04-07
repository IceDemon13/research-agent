using Telemart.Common.ErrorHandling;

namespace Telemart.Client.Data.Requests.Base
{
    public abstract class UpdateEntityResultRequestBase<T, TUpdateDto> : UpdateEntityRequestBase<Result<T>, TUpdateDto>
    {
        protected UpdateEntityResultRequestBase(TUpdateDto dto, params object[] pathParameters)
            : base(dto, pathParameters)
        {
        }
    }
}