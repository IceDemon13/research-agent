using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Telemart.Client.Reports.ReportBuilders.Base;

namespace Telemart.Client.Extensions.ContainerExtensions
{
    public static class ContainerReportPrintersExtensions
    {
        public static IServiceCollection RegisterReportPrinters(this IServiceCollection serviceCollection, Assembly assembly)
        {
            Type[] types = assembly
                .GetTypes()
                .Where(t => t.IsClass && t.BaseType != null && t.BaseType.IsGenericType && t.BaseType.GetGenericTypeDefinition() == typeof(ReportPrinterBase<>))
                .ToArray();

            foreach (Type type in types)
            {
                Type interfaceType = type.GetInterfaces().FirstOrDefault(x => x.IsGenericType == false);

                if (interfaceType != null)
                {
                    serviceCollection.AddSingleton(interfaceType, type);
                }
            }

            return serviceCollection;
        }
    }
}