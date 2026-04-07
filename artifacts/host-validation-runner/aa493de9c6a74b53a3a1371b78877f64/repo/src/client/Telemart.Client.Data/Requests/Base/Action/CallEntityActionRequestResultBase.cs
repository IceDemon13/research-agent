using Telemart.Common.ErrorHandling;

namespace Telemart.Client.Data.Requests.Base.Action;

public abstract class CallEntityActionRequestResultBase<T> : CallEntityActionRequestBase<Result<T>>
    where T : class
{
    protected CallEntityActionRequestResultBase(object id, string resource, string methodName)
        : base(id, resource, methodName)
    {
    }
}