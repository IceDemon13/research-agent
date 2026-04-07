using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Mvvm.UI;
using Microsoft.Extensions.Logging;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.PrivatBank;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Validation;

namespace Telemart.Client.ViewModels.Money.Receive.ImportBankPayments
{
    public sealed class ImportPageViewModel : WizardPageViewModelBase<ImportBankPaymentsModel>, ISupportWizardFinishCommand
    {
        public ImportPageViewModel(IWebClient webClient, ILogger<ImportPageViewModel> logger)
        {
            WebClient = webClient;
            Logger = logger;
            HandleLoadedCommand = new AsyncCommand(HandleLoadedAsync);
        }

        public ImportPageViewModel()
        {
        }

        #region Commands

        public IAsyncCommand HandleLoadedCommand { get; }

        #endregion

        public bool CanFinish => !IsLongOperationInProgress;

        public override string Description { get; } = "Не закрывайте диалог до окончания операции";

        public override string Header { get; } = "Импорт оплат";

        private CurrentWindowService CurrentWindowService => (CurrentWindowService)GetService<ICurrentWindowService>(ServiceSearchMode.PreferParents);

        private IWebClient WebClient { get; }

        private ILogger<ImportPageViewModel> Logger { get; }

        public void OnFinish(CancelEventArgs e)
        {
        }

        private async Task HandleLoadedAsync()
        {
            CurrentWindowService.ActualWindow.Closing += ActualWindowClosing;

            IsLongOperationInProgress = true;

            try
            {
                try
                {
                    Model.Result = await WebClient.ExecuteApiRequestAsync(new ImportPaymentsAutoclient(Model.CashboxId));

                    if (Model.Result.Warnings?.Any() == true)
                    {
                        Model.ValidationItems = Model.Result.Warnings.Select(x => new ValidationResultItem(x, false)).ToObservableCollection();
                    }
                }
                catch (UnexpectedSatusException exception)
                {
                    Model.ValidationItems = new ObservableCollection<ValidationResultItem>(exception.GetErrorItems());
                }
                catch (UnexpectedErrorException exception)
                {
                    Logger.LogError(exception, "Failed to import payments");
                    Model.ValidationItems = new ObservableCollection<ValidationResultItem> { new ValidationResultItem(Resources.ServerUnavailable, true) };
                }
                catch (Exception exception)
                {
                    Logger.LogError(exception, "Failed to import payments");
                    Model.ValidationItems = new ObservableCollection<ValidationResultItem> { new ValidationResultItem("Непредвиденная ошибка", true) };
                }
            }
            finally
            {
                CurrentWindowService.ActualWindow.Closing -= ActualWindowClosing;
                IsLongOperationInProgress = false;
            }
        }

        private void ActualWindowClosing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
        }
    }
}