using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Data.Requests.Features.Cashbox;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Helpers;
using Telemart.Client.TransferObjects.Cashbox;

namespace Telemart.Client.ViewModels.Money.Receive.BankPayment
{
    public sealed class BankPaymentsFilterViewModel : BindableBase, IDataErrorInfo
    {
        private List<CashboxDto> cashboxesList;

        public BankPaymentsFilterViewModel(IWebClient webClient, IDictionaries dictionaries)
        {
            WebClient = webClient ?? throw new ArgumentNullException(nameof(webClient));
            Dictionaries = dictionaries ?? throw new ArgumentNullException(nameof(dictionaries));

            Statuses = Dictionaries.GetItems<BankPaymentState>().ToReadOnlyObservableCollection();
        }

        public BankPaymentsFilterViewModel()
        {
        }

        #region Collections

        public ReadOnlyObservableCollection<ComboBoxItem> Cashboxes
        {
            get { return GetProperty(() => Cashboxes); }
            set { SetProperty(() => Cashboxes, value); }
        }

        public ReadOnlyObservableCollection<BankPaymentState> Statuses
        {
            get { return GetProperty(() => Statuses); }
            set { SetProperty(() => Statuses, value); }
        }

        #endregion

        public DateTime? CreatedFrom
        {
            get { return GetProperty(() => CreatedFrom); }
            set { SetProperty(() => CreatedFrom, value); }
        }

        public DateTime? CreatedTo
        {
            get { return GetProperty(() => CreatedTo); }
            set { SetProperty(() => CreatedTo, value); }
        }

        public string OrderIds
        {
            get { return GetProperty(() => OrderIds); }
            set { SetProperty(() => OrderIds, value); }
        }

        public List<object> SelectedCashboxes
        {
            get { return GetProperty(() => SelectedCashboxes); }
            set { SetProperty(() => SelectedCashboxes, value); }
        }

        public List<object> SelectedStatuses
        {
            get { return GetProperty(() => SelectedStatuses); }
            set { SetProperty(() => SelectedStatuses, value); }
        }

        string IDataErrorInfo.Error => string.Empty;

        private IDictionaries Dictionaries { get; }

        private IWebClient WebClient { get; }

        string IDataErrorInfo.this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        public static void BuildMetadata(MetadataBuilder<BankPaymentsFilterViewModel> builder)
        {
            builder.Property(x => x.OrderIds)
                .MatchesRegularExpression(@"^(\d+(,|,\d+)*)?$", () => "Допускаются только целыe числа, разделенные запятой");
        }

        public BankPaymentFilteringItem GetFilteringItem()
        {
            BankPaymentFilteringItem item = new BankPaymentFilteringItem
            {
                OrderIds = OrderIds,
                Cashboxes = SelectedCashboxes?.Cast<int>().ToList(),
                CreatedFrom = CreatedFrom,
                CreatedTo = CreatedTo,
                Statuses = SelectedStatuses?.Cast<int>().ToList()
            };

            return item;
        }

        public Task RefreshAsync()
        {
            return RefreshCashboxesAsync();
        }

        public void ResetFilterValues()
        {
            CreatedFrom = null;
            CreatedTo = null;
            SelectedStatuses = new List<object> { BankPaymentState.NewId };
            OrderIds = null;
            SelectedCashboxes = null;
        }

        private async Task RefreshCashboxesAsync()
        {
            List<CashboxDto> cashboxes = await WebClient.ExecuteApiRequestAsync(new QueryCashboxes(), true);

            if (ReferenceEquals(cashboxes, cashboxesList))
            {
                return;
            }

            cashboxesList = cashboxes;

            Cashboxes = cashboxes.ForIncome(WebClient, Currency.Uah.Id, Payment.BankId)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();
        }
    }
}