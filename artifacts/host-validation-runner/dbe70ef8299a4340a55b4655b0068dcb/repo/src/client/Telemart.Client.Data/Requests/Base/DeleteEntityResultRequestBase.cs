using Telemart.Common.ErrorHandling;

namespace Telemart.Client.Data.Requests.Base
{
    public abstract class DeleteEntityResultRequestBase<T> : DeleteEntityRequestBase<Result<T>>
        where T : class, new()
    {
        protected DeleteEntityResultRequestBase(params object[] pathParameters)
            : base(pathParameters)
        {
        }
    }
}