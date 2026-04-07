using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Telemart.Client.Cache;
using Telemart.Client.Cache.Synchronization.Base;

namespace Telemart.Client.Extensions.ContainerExtensions
{
    public static class ContainerSyncServiceExtensions
    {
        public static void RegisterSyncServices(this IServiceCollection serviceCollection)
        {
            Type[] types = typeof(ICache).Assembly
                .GetTypes()
                .Where(t => t.IsClass
                            && !t.IsAbstract
                            && t.BaseType is { IsGenericType: true }
                            && (t.BaseType.GetGenericTypeDefinition() == typeof(SyncServiceBase<,>)))
                .ToArray();

            foreach (Type type in types)
            {
                Type interfaceType = type.GetInterfaces().FirstOrDefault(x => x.IsGenericType == false);

                if (interfaceType != null)
                {
                    serviceCollection.AddSingleton(interfaceType, type);
                }
            }
        }
    }
}