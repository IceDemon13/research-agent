using System;
using System.Linq;
using System.Reflection;
using DevExpress.Mvvm;
using Microsoft.Extensions.DependencyInjection;

namespace Telemart.Client.Extensions.ContainerExtensions;

public static class ContainerViewModelExtensions
{
    public static void RegisterViewModels(this IServiceCollection serviceCollection, Assembly assembly)
    {
        Type[] viewModelTypes = assembly
            .GetTypes()
            .Where(t => t.IsClass
                        && !t.IsAbstract
                        && t.IsSubclassOf(typeof(ViewModelBase)) && t != typeof(ViewModelBase))
            .ToArray();

        foreach (Type implementation in viewModelTypes)
        {
            serviceCollection.AddTransient(implementation);
        }
    }
}