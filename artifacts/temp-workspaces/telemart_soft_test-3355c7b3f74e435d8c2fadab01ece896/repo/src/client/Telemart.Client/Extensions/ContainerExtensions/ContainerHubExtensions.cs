using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Telemart.Client.Data.HubClient.Base;

namespace Telemart.Client.Extensions.ContainerExtensions
{
    public static class ContainerHubExtensions
    {
        public static IServiceCollection RegisterHubs(this IServiceCollection serviceCollection, Assembly assembly)
        {
            serviceCollection.AddSingleton(typeof(IHubClientBase<>), typeof(HubClientBase<>));

            Type[] hubClientTypes = assembly
                .GetTypes()
                .Where(t => t.IsClass
                            && !t.IsAbstract
                            && t.BaseType is { IsGenericType: true }
                            && (t.BaseType.GetGenericTypeDefinition() == typeof(HubClientBase<>)))
                .ToArray();

            foreach (Type implementation in hubClientTypes)
            {
                serviceCollection.AddTransient(implementation);
            }

            return serviceCollection;
        }
    }
}