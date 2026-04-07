using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Mvvm.UI;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.AccountingSystem;
using Telemart.Client.Data.Requests.Features.Currency;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Prices;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.WebClient.Prices;

namespace Telemart.Client.ViewModels.Common
{
    public sealed class UpdateCurrencyRatesViewModel : TelemartDialogViewModelBase
    {
        private bool _loadingFinished;
        
        public UpdateCurrencyRatesViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IPricesClient pricesClient,
            IMessenger mesenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Mesenger = mesenger;
            PricesClient = pricesClient;

            HandleSelectionChangedCommand = new DelegateCommand(HandleSelectionChanged);
        }

        public UpdateCurrencyRatesViewModel()
        {
        }

        public IDelegateCommand HandleSelectionChangedCommand { get; }

        public CurrencyTypeRateViewItem SelectedToUahCurrencyTypeRate
        {
            get { return GetProperty(() => SelectedToUahCurrencyTypeRate); }
            set { SetProperty(() => SelectedToUahCurrencyTypeRate, value); }
        }

        public ObservableCollection<CurrencyTypeRateViewItem> ToUahCurrencyTypeRates
        {
            get { return GetProperty(() => ToUahCurrencyTypeRates); }
            set { SetProperty(() => ToUahCurrencyTypeRates, value); }
        }

        public ObservableCollection<CurrencyTypeRateViewItem> OtherCurrencyTypeRates
        {
            get { return GetProperty(() => OtherCurrencyTypeRates); }
            set { SetProperty(() => OtherCurrencyTypeRates, value); }
        }

        private CurrentWindowService CurrentWindowService => (CurrentWindowService)GetService<ICurrentWindowService>();

        private IMessenger Mesenger { get; }

        private IPricesClient PricesClient { get; }

        protected override async Task HandleLoadedAsync()
        {
            List<CurrencyTypeRateDto> currencyTypeRates = await WebClient.ExecuteApiRequestAsync(new QueryCurrencyTypeRates());

            ToUahCurrencyTypeRates = currencyTypeRates
                .Where(x => x.ToCurrencyTypeId == CurrencyTypeIds.UahId)
                .Select(x => new CurrencyTypeRateViewItem()
                {
                    Id = x.Id,
                    FromCurrencyId = x.FromCurrencyId,
                    FromCurrencyTypeId = x.FromCurrencyTypeId,
                    ToCurrencyTypeId = x.ToCurrencyTypeId,
                    ConversionRate = x.ConversionRate,
                    ConversionRateOld = x.ConversionRate,
                    ConversionRateInitial = x.ConversionRate,
                    Name = x.Name
                }).ToObservableCollection();

            OtherCurrencyTypeRates = currencyTypeRates
                .Where(x => x.ToCurrencyTypeId != CurrencyTypeIds.UahId)
                .Select(x => new CurrencyTypeRateViewItem()
                {
                    Id = x.Id,
                    FromCurrencyId = x.FromCurrencyId,
                    FromCurrencyTypeId = x.FromCurrencyTypeId,
                    ToCurrencyTypeId = x.ToCurrencyTypeId,
                    ConversionRate = x.ConversionRate,
                    ConversionRateOld = x.ConversionRate,
                    ConversionRateInitial = x.ConversionRate,
                    Name = x.Name
                }).ToObservableCollection();

            Title = "Курсы";

            _loadingFinished = true;
        }

        protected override async Task HandleOkAsync()
        {
            if (OtherCurrencyTypeRates.Count(x => x.ConversionRateInitialChanged) < ToUahCurrencyTypeRates.Count(x => x.ConversionRateInitialChanged))
            {
                MessageFacadeService.ShowNotificationError("Не все кросс-курсы пересчитаны");
                return;
            }

            DelayedConfirmViewModel viewModel = DialogDocumentManagerService.ShowView<DelayedConfirmViewModel>("Вы уверены?", this);

            if (!viewModel.IsOk)
            {
                return;
            }

            CurrentWindowService.ActualWindow.Closing += ActualWindowOnClosing;

            bool updated;

            try
            {
                CurrencyTypeRateSaveDto[] saveDtos = ToUahCurrencyTypeRates
                    .Where(x => x.ConversionRateInitialChanged)
                    .Select(x => new CurrencyTypeRateSaveDto(x.Id, x.ConversionRate))
                    .Union(OtherCurrencyTypeRates.Where(x => x.ConversionRateInitialChanged).Select(x => new CurrencyTypeRateSaveDto(x.Id, x.ConversionRate)))
                    .ToArray();

                if (!saveDtos.Any())
                {
                    MessageFacadeService.ShowNotificationWarning("Нечего сохранять");
                    return;
                }

                updated = await UpdateRatesAsync(saveDtos);

                if (updated)
                {
                    updated = await UpdateRatesInAccountingSystemAsync();
                }
            }
            finally
            {
                CurrentWindowService.ActualWindow.Closing -= ActualWindowOnClosing;
            }

            if (updated)
            {
                List<CurrencyTypeRateDto> currencyTypeRates = await WebClient.ExecuteApiRequestAsync(new QueryCurrencyTypeRates());

                Mesenger.Send(new UpdateCurrencyRatesMessage(currencyTypeRates));

                MessageFacadeService.ShowNotificationInfo("Курсы обновлены успешно");

                IsOk = true;
                Close();
            }
        }

        private async Task<bool> UpdateRatesAsync(IReadOnlyCollection<CurrencyTypeRateSaveDto> saveDtos)
        {
            bool result = false;

            (await PricesClient.SaveRatesAsync(new SaveRatesRequest(saveDtos), this)).IfNotNull(_ => result = true);

            return result;
        }

        private async Task<bool> UpdateRatesInAccountingSystemAsync()
        {
            bool result = false;

            CurrencyTypeRateSaveDto[] saveDtosForAccountingSystem = ToUahCurrencyTypeRates
                .GroupBy(x => x.FromCurrencyId)
                .Where(x => x.Any(z => z.ConversionRateChanged))
                .SelectMany(x => x)
                .Select(x => new CurrencyTypeRateSaveDto(x.Id, x.ConversionRate))
                .ToArray();

            if (!saveDtosForAccountingSystem.Any())
            {
                return true;
            }

            try
            {
                await WebClient.ExecuteApiRequestAsync(new SaveConversionRatesToAccountingSystem(new SaveRatesRequest(saveDtosForAccountingSystem)));
                result = true;
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed while update 1C");
                MessageFacadeService.ShowNotificationError("Ошибка при обновлении курсов в 1С");
            }

            return result;
        }

        private void ActualWindowOnClosing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
        }

        private void HandleSelectionChanged()
        {
            if (SelectedToUahCurrencyTypeRate is null || !_loadingFinished)
            {
                return;
            }

            CurrencyTypeRateViewItem otherCurrencyTypeRate = OtherCurrencyTypeRates
                .FirstOrDefault(x => x.FromCurrencyTypeId == SelectedToUahCurrencyTypeRate.ToCurrencyTypeId
                                     && x.ToCurrencyTypeId == SelectedToUahCurrencyTypeRate.FromCurrencyTypeId);

            if (otherCurrencyTypeRate is null
                || otherCurrencyTypeRate.AutoConvertedFromConversionRate == SelectedToUahCurrencyTypeRate.ConversionRate
                || !SelectedToUahCurrencyTypeRate.ConversionRateChanged)
            {
                return;
            }

            SelectedToUahCurrencyTypeRate.ConversionRateOld = SelectedToUahCurrencyTypeRate.ConversionRate;

            otherCurrencyTypeRate.ConversionRate = decimal.Round(1 / SelectedToUahCurrencyTypeRate.ConversionRate, 25);
            otherCurrencyTypeRate.AutoConvertedFromConversionRate = SelectedToUahCurrencyTypeRate.ConversionRate;
        }
    }
}