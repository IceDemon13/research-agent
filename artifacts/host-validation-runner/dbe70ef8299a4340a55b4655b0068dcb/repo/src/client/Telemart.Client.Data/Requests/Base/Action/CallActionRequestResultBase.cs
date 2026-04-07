using Telemart.Client.TransferObjects;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.Data.Requests.Base.Action
{
    public abstract class CallActionRequestResultBase<T> : CallActionRequestBase<Result<T>>
        where T : class
    {
        protected CallActionRequestResultBase(string resource, string methodName)
            : base(resource, methodName)
        {
        }
    }
}