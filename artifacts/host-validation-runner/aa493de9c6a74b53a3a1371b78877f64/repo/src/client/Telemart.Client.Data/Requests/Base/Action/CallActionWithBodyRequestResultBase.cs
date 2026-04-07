using Telemart.Client.TransferObjects;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.Data.Requests.Base.Action
{
    public abstract class CallActionWithBodyRequestResultBase<TResult, T> : CallActionWithBodyRequestBase<Result<TResult>, T>
        where TResult : class
    {
        protected CallActionWithBodyRequestResultBase(T dto, string resource, string methodName)
            : base(dto, resource, methodName)
        {
        }
    }
}