using System;
using Microsoft.Extensions.DependencyInjection;
using Telemart.Client.Business.Delivery;
using Telemart.Client.Business.Delivery.CityProviders;
using Telemart.Client.Business.Delivery.TrackNumberProviders;
using Telemart.Client.Dictionaries;

namespace Telemart.Client.Extensions
{
    public static class CarryTypeExtensions
    {
        public static IServiceProvider ServiceProvider { get; set; }

        public static ITrackNumberProvider GetTrackNumberProvider(this CarryType carryType)
        {
            Type providerType;

            if (carryType.IsNovaposhta())
            {
                providerType = typeof(NpTrackNumberProvider);
            }
            else if (carryType.IsUkrposhta())
            {
                providerType = typeof(UpTrackNumberProvider);
            }
            else if (carryType.IsMeestExpress())
            {
                providerType = typeof(MeTrackNumberProvider);
            }
            else if (carryType.Id == CarryType.GabaritkaId)
            {
                providerType = typeof(GabaritkaTrackNumberProvider);
            }
            else if (carryType.Id is CarryType.SmartPostId or CarryType.HomenkoId)
            {
                providerType = typeof(TelemartTrackNumberProvider);
            }
            else if (carryType.Id == CarryType.TeksId)
            {
                providerType = typeof(TeksTrackNumberProvider);
            }
            else
            {
                providerType = typeof(NotImplementedTrackNumberProvider);
            }

            return (ITrackNumberProvider)ServiceProvider.GetRequiredService(providerType);
        }

        public static IDeliveryServiceCityProvider GetCityProvider(this CarryType carryType)
        {
            Type providerType = null;

            if (carryType.IsNovaposhta())
            {
                providerType = typeof(NpCityProvider);
            }
            else if (carryType.IsMeestExpress())
            {
                providerType = typeof(MeCityProvider);
            }
            else if (carryType.IsUkrposhta())
            {
                providerType = typeof(UpCityProvider);
            }
            else if (carryType.Id == CarryType.UklonId)
            {
                providerType = typeof(UklonCityProvider);
            }

            return (IDeliveryServiceCityProvider)ServiceProvider.GetService(providerType!);
        }

        public static bool IsUpCarryId(this int carryId)
        {
            return carryId == CarryType.UpDeliveryId || carryId == CarryType.UpWarehouseId;
        }

        public static bool IsTeksCarry(this int carryId)
        {
            return carryId == CarryType.TeksId;
        }
    }
}