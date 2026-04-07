using Microsoft.Extensions.DependencyInjection;
using Telemart.Client.Business.Delivery.CityProviders;
using Telemart.Client.Business.Delivery.TrackNumberProviders;

namespace Telemart.Client.Extensions.ContainerExtensions
{
    public static class ContainerCarryExtensions
    {
        public static IServiceCollection RegisterCarryProviders(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<NpTrackNumberProvider>();
            serviceCollection.AddSingleton<UpTrackNumberProvider>();
            serviceCollection.AddSingleton<MeTrackNumberProvider>();
            serviceCollection.AddSingleton<GabaritkaTrackNumberProvider>();
            serviceCollection.AddSingleton<TelemartTrackNumberProvider>();
            serviceCollection.AddSingleton<TeksTrackNumberProvider>();
            serviceCollection.AddSingleton<NotImplementedTrackNumberProvider>();

            serviceCollection.AddSingleton<NpCityProvider>();
            serviceCollection.AddSingleton<MeCityProvider>();
            serviceCollection.AddSingleton<UpCityProvider>();
            serviceCollection.AddSingleton<UklonCityProvider>();

            return serviceCollection;
        }
    }
}