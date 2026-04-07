namespace Telemart.Client.Data.Requests.Base.Action
{
    public abstract class CallActionWithBodyRequestBase<TResult, T> : CallActionRequestBase<TResult>
        where TResult : class
    {
        protected CallActionWithBodyRequestBase(T dto, string resource, string methodName)
            : base(resource, methodName)
        {
            Body = dto;
        }
    }
}