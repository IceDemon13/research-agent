using System;
using Microsoft.Extensions.DependencyInjection;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.HubClient.Base;
using Telemart.Client.Extensions;

namespace Telemart.Client.Common.HubFactory
{
    public class HubClientFactory : IHubClientFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public HubClientFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IHubClientBase<T> Create<T>(IMessageFacadeService messageFacadeService, bool exceptionHandling, bool stateHandling)
            where T : HubClientBase<T>
        {
            T instance = _serviceProvider.GetService<T>();

            if (exceptionHandling)
            {
                instance.WithExceptionHandling(messageFacadeService);
            }

            if (stateHandling)
            {
                instance.WithStateHandling(messageFacadeService);
            }

            return instance;
        }
    }
}