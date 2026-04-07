namespace Telemart.Client.Data.Requests.Base.Action
{
    public abstract class CallEntityActionWithBodyRequestResultBase<TResult, TBody> : CallEntityActionRequestResultBase<TResult>
        where TResult : class
    {
        protected CallEntityActionWithBodyRequestResultBase(object id, TBody dto, string resource, string methodName)
            : base(id, resource, methodName)
        {
            Body = dto;
        }
    }
}