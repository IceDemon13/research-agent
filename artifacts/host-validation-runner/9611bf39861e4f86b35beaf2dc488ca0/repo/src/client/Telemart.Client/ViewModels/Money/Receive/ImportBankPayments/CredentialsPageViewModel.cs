using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Data.Requests.Features.Cashbox;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects.Cashbox;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Money.Receive.ImportBankPayments
{
    public sealed class CredentialsPageViewModel :
        WizardPageViewModelBase<ImportBankPaymentsModel>,
        ISupportWizardNextCommand
    {
        public CredentialsPageViewModel(IWebClient webClient)
        {
            WebClient = webClient;
            HandleLoadedCommand = new AsyncCommand(HandleLoadedAsync);
        }

        #region Commands

        public IAsyncCommand HandleLoadedCommand { get; }

        #endregion

        public bool CanGoForward => Model != null;

        public override string Description { get; } = "Введите учетные данные для импорта";

        public override string Header { get; } = "Учетные данные";

        public ReadOnlyObservableCollection<ComboBoxItem> Cashboxes
        {
            get { return GetProperty(() => Cashboxes); }
            private set { SetProperty(() => Cashboxes, value); }
        }

        private IWizardService WizardService => GetService<IWizardService>();

        private IWebClient WebClient { get; }

        public void OnGoForward(CancelEventArgs e)
        {
            WizardService.NavigateToView<ImportPageViewModel>(Model, ((ISupportParentViewModel)this).ParentViewModel);
        }

        private async Task HandleLoadedAsync()
        {
            List<CashboxDto> cashboxes = await WebClient.ExecuteApiRequestAsync(new QueryCashboxes(), true);

            Cashboxes = cashboxes
                .Where(x => WebClient.AuthenticatedEmployee.AllowCashboxes.Contains(x.Id) && x.IsActive && x.CurrencyId == Currency.Uah.Id && x.HasAccount)
                .OrderBy(x => x.Name)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();
        }
    }
}