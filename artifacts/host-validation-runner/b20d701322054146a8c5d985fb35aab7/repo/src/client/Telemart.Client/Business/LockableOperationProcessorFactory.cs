using System;
using Microsoft.Extensions.DependencyInjection;

namespace Telemart.Client.Business
{
    public sealed class LockableOperationProcessorFactory : ILockableOperationProcessorFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public LockableOperationProcessorFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public LockableOperationProcessor<TDto> Create<TDto>()
            where TDto : class, new()
        {
            return _serviceProvider.GetRequiredService<LockableOperationProcessor<TDto>>();
        }
    }
}