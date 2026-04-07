using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Hashtag;
using Telemart.Client.Data.Requests.Features.Hashtag.Actions;
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Store.Order
{
    public class OrderConfigurationViewModel : TelemartDialogViewModelBase
    {
        public OrderConfigurationViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IErrorHandler errorHandler)
            : base(webClient, dictionaries, messageFacadeService)
        {
            ErrorHandler = errorHandler;
        }

        public int Amount
        {
            get { return GetProperty(() => Amount); }
            set { SetProperty(() => Amount, value); }
        }

        public ObservableCollection<int> SelectedHashtagIds
        {
            get { return GetProperty(() => SelectedHashtagIds); }
            set { SetProperty(() => SelectedHashtagIds, value); }
        }

        public ObservableCollection<int> SelectedPaymentIds
        {
            get { return GetProperty(() => SelectedPaymentIds); }
            set { SetProperty(() => SelectedPaymentIds, value); }
        }

        public ObservableCollection<int> SelectedCarryIds
        {
            get { return GetProperty(() => SelectedCarryIds); }
            set { SetProperty(() => SelectedCarryIds, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Carries
        {
            get { return GetProperty(() => Carries); }
            private set { SetProperty(() => Carries, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Payments
        {
            get { return GetProperty(() => Payments); }
            private set { SetProperty(() => Payments, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Hashtags
        {
            get { return GetProperty(() => Hashtags); }
            private set { SetProperty(() => Hashtags, value); }
        }

        private IErrorHandler ErrorHandler { get; }

        public static void BuildMetadata(MetadataBuilder<OrderConfigurationViewModel> builder)
        {
            builder.Property(x => x.Amount)
                .MatchesInstanceRule((x, y) => x >= 0, () => "Значение должно быть положительным");
        }

        protected override async Task HandleLoadedAsync()
        {
            Carries = Dictionaries
                .GetItems<CarryType>()
                .Where(x => x.Active)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .OrderBy(x => x.DisplayValue)
                .ToReadOnlyObservableCollection();

            Payments = Dictionaries.GetItems<Payment>()
                .Where(x => x.Active)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .OrderBy(x => x.DisplayValue)
                .ToReadOnlyObservableCollection();

            List<HashtagDto> hashtags = await WebClient.ExecuteApiRequestAsync(new QueryHashtags());

            Hashtags = hashtags
                .Where(x => x.Active)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .OrderBy(x => x.DisplayValue)
                .ToReadOnlyObservableCollection();

            OrderConfigurationDto dto = await WebClient.ExecuteApiRequestAsync(new QueryOrderConfiguration());

            Amount = dto.DontCallMeAmount;
            SelectedCarryIds = dto.DontCallMeCarryIds?.ToObservableCollection() ?? new();
            SelectedHashtagIds = dto.DontCallMeHashtagIds?.ToObservableCollection() ?? new();
            SelectedPaymentIds = dto.DontCallMePaymentIds?.ToObservableCollection() ?? new();

            await base.HandleLoadedAsync();

            Title = "Настройки корзины";
        }

        protected override async Task HandleOkAsync()
        {
            OrderConfigurationDto saveDto = new OrderConfigurationDto()
            {
                DontCallMeAmount = Amount,
                DontCallMeCarryIds = SelectedCarryIds.ToList(),
                DontCallMeHashtagIds = SelectedHashtagIds.ToList(),
                DontCallMePaymentIds = SelectedPaymentIds.ToList()
            };

            (await ErrorHandler.HandleErrorsAsync(x => WebClient.ExecuteApiRequestAsync(new UpdateOrderConfiguration(saveDto)), "сохранении настроек", "Настройки сохранены", this, true))
                .IfNotNull(x =>
                {
                    IsOk = true;
                    Close();
                });
        }
    }
}