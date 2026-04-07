using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Helpers;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Store.Invoice
{
    public sealed class InvoiceCurrencyRatesViewModel : TelemartDialogViewModelBase
    {
        private TelemartEnumerableCompareHelper<InvoiceCurrencyRateViewItem> compareHelper;

        public InvoiceCurrencyRatesViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
            CurrencyRates = new ObservableCollection<InvoiceCurrencyRateViewItem>();
        }

        public ObservableCollection<InvoiceCurrencyRateViewItem> CurrencyRates
        {
            get { return GetProperty(() => CurrencyRates); }
            private init { SetProperty(() => CurrencyRates, value); }
        }

        protected override async Task HandleLoadedAsync()
        {
            InvoiceCurrencyRatesParameter parameter = (InvoiceCurrencyRatesParameter)Parameter;

            IReadOnlyCollection<Currency> currencies = Dictionaries
                .GetItems<Currency>()
                .Where(x => x.Id != Currency.UahId)
                .ToArray();

            foreach (Currency currency in currencies)
            {
                InvoiceCurrencyRateDto dto = parameter.CurrencyRates?
                    .FirstOrDefault(x => x.FromCurrencyId == currency.Id);

                CurrencyRates.Add(new InvoiceCurrencyRateViewItem()
                {
                    Id = dto?.Id ?? 0,
                    Name = currency.Title,
                    FromCurrencyId = currency.Id,
                    ConversionRate = dto?.ConversionRate
                });
            }

            compareHelper = new TelemartEnumerableCompareHelper<InvoiceCurrencyRateViewItem>(CurrencyRates);

            Title = $"Курсы по накладной №{parameter.InvoiceId}";

            await base.HandleLoadedAsync();
        }

        protected override Task HandleOkAsync()
        {
            if (!compareHelper.IsChanged())
            {
                MessageFacadeService.ShowNotificationWarning("Ничего не поменялось");
                return Task.CompletedTask;
            }

            if (CurrencyRates.Any(x => IDataErrorInfoHelper.HasErrors(x)))
            {
                MessageFacadeService.ShowMessageBoxError("В таблице присутствуют недопустимые курсы валют");
                return Task.CompletedTask;
            }

            CloseOk();

            return Task.CompletedTask;
        }
    }
}