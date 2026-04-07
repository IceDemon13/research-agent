namespace Telemart.Client.Business
{
    public interface ILockableOperationProcessorFactory
    {
        LockableOperationProcessor<TDto> Create<TDto>()
            where TDto : class, new();
    }
}