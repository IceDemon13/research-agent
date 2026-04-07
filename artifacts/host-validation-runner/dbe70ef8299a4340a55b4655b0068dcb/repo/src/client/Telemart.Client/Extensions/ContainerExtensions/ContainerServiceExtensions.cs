using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Telemart.Client.Core.Update;
using Telemart.Client.Data;
using Telemart.Client.Data.Authentication;
using Telemart.Client.Data.Options;

namespace Telemart.Client.Extensions.ContainerExtensions;

public static class ContainerServiceExtensions
{
    public static IServiceCollection AddService<TOptions>(
        this IServiceCollection serviceCollection,
        IConfiguration configuration,
        Services service,
        bool withAuthorization = true)
    where TOptions : ServiceOptionsBase
    {
        serviceCollection.AddOptions<TOptions>(configuration);

        IHttpClientBuilder httpClientBuilder = serviceCollection.AddHttpClient(service.ToString(), (sp, client) =>
        {
            TOptions options = sp.GetRequiredService<TOptions>();

            IUpdateManager updateManager = sp.GetRequiredService<IUpdateManager>();

            client.DefaultRequestHeaders.Add(Headers.UserAgent, "telemart.client");

            client.DefaultRequestHeaders.Add(Headers.UserAgentVersion, updateManager.GetCurrentAssemblyVersion().ToString());

            client.BaseAddress = new Uri(options.BaseAddress);
            client.Timeout = TimeSpan.FromSeconds(options.DefaultTimeoutSeconds);
        });

        if (withAuthorization)
        {
            serviceCollection.AddTransient<AuthenticationHttpClientMessageHandler>();

            httpClientBuilder.AddHttpMessageHandler<AuthenticationHttpClientMessageHandler>();
        }

        return serviceCollection;
    }

    public static IServiceCollection AddOptions<TOptions>(
        this IServiceCollection serviceCollection,
        IConfiguration configuration)
    where TOptions : class
    {
        serviceCollection.Configure<TOptions>(configuration);
        serviceCollection.AddTransient(x => x.GetRequiredService<IOptionsMonitor<TOptions>>().CurrentValue);

        return serviceCollection;
    }
}