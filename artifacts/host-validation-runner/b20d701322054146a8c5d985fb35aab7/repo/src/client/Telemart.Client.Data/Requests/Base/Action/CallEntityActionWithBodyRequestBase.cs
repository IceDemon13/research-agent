namespace Telemart.Client.Data.Requests.Base.Action;

public class CallEntityActionWithBodyRequestBase<TResult, TBody> : CallEntityActionRequestBase<TResult>
    where TResult : class
{
    protected CallEntityActionWithBodyRequestBase(object id, TBody dto, string resource, string methodName)
        : base(id, resource, methodName)
    {
        Body = dto;
    }
}