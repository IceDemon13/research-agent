using System;
using System.Threading.Tasks;
using Telemart.Client.Business.Delivery.Data;

namespace Telemart.Client.Business.Delivery
{
    public interface ITrackNumberProvider
    {
        bool SupportTrackNumbers { get; }

        bool SupportGetDeliveryDate { get; }

        Task<string> CreateAsync(int orderId, PackageProperties package);

        Task PrintAsync(string trackNumber, bool showPreview);

        Task TrackAsync(int orderId, string packageTtn);

        Task<DateTime> GetDeliveryDateAsync(int carryTypeId, int citySenderId, int cityRecipientId, DateTime startDate);
    }
}