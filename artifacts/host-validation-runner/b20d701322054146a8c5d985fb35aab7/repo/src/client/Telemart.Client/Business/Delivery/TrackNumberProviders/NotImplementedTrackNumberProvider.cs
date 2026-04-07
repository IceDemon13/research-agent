using System;
using System.Threading.Tasks;
using Telemart.Client.Business.Delivery.Data;
using Telemart.Client.Common.Services;

namespace Telemart.Client.Business.Delivery.TrackNumberProviders
{
    public sealed class NotImplementedTrackNumberProvider : ITrackNumberProvider
    {
        public NotImplementedTrackNumberProvider(IMessageFacadeService messageFacadeService)
        {
            MessageFacadeService = messageFacadeService;
        }

        public bool SupportTrackNumbers { get; } = false;

        public bool SupportGetDeliveryDate { get; } = false;

        private IMessageFacadeService MessageFacadeService { get; }

        public Task<string> CreateAsync(int orderId, PackageProperties package)
        {
            throw new NotImplementedException();
        }

        public Task PrintAsync(string trackNumber, bool showPreview)
        {
            MessageFacadeService.ShowNotificationWarning("Невозможно напечатать ТТН для данного типа доставки");

            return Task.CompletedTask;
        }

        public Task TrackAsync(int orderId, string packageTtn)
        {
            MessageFacadeService.ShowNotificationWarning("Невозможно отследить ТТН для данного типа доставки");

            return Task.CompletedTask;
        }

        public Task<DateTime> GetDeliveryDateAsync(int carryTypeId, int citySenderId, int cityRecipientId, DateTime startDate)
        {
            throw new NotSupportedException();
        }
    }
}