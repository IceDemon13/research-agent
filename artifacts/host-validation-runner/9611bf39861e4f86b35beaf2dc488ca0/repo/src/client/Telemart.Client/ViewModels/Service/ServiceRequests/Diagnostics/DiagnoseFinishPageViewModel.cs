using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Messages;
using Telemart.Client.Extensions;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Validation;

namespace Telemart.Client.ViewModels.Service.ServiceRequests.Diagnostics
{
    public sealed class DiagnoseFinishPageViewModel :
        WizardPageViewModelBase<DiagnoseServiceRequestModel>,
        ISupportWizardFinishCommand,
        ISupportWizardBackCommand
    {
        private IDocumentManagerService SizeableDialogDocumentManagerService => GetService<IDocumentManagerService>("SizeableDialogDocumentManagerService", ServiceSearchMode.PreferParents);

        public DiagnoseFinishPageViewModel(IMessenger messenger, ILogger<DiagnoseFinishPageViewModel> logger)
        {
            Messenger = messenger;
            Logger = logger;

            HandleLoadedCommand = new AsyncCommand(HandleLoadedAsync);
            WindowClosingCommand = new DelegateCommand<CancelEventArgs>(WindowClosing);
        }

        public DiagnoseFinishPageViewModel()
        {
        }

        #region Commands

        public IAsyncCommand HandleLoadedCommand { get; }

        public IDelegateCommand WindowClosingCommand { get; }

        #endregion

        public bool CanFinish => true;

        public bool CanGoBack => false;

        public override string Description => Model.ValidationItems != null && Model.ValidationItems.Any()
            ? "Ошибки при обработке заявки"
            : $"Заявка №{Model.Result.Id} успешно обработана";

        public override string Header { get; } = "Результат";

        private IMessenger Messenger { get; }

        private ILogger<DiagnoseFinishPageViewModel> Logger { get; }

        public void OnFinish(CancelEventArgs e)
        {
        }

        public void OnGoBack(CancelEventArgs e)
        {
            throw new System.NotImplementedException();
        }

        private Task HandleLoadedAsync()
        {
            if (Model.Result != null)
            {
                Messenger.Send(new ServiceRequestMessage(Model.Result, MessageType.Changed));
            }

            return Task.CompletedTask;
        }

        private void WindowClosing(CancelEventArgs e)
        {
            ShowWarnigs();
        }

        private void ShowWarnigs()
        {
            if (Model.Warnings?.Any() == true)
            {
                SizeableDialogDocumentManagerService.ShowView<ValidationResultViewModel>(
                    new ValidationResultViewModelParameter("Предупреждения", Model.Warnings),
                    this);

                Logger.LogWarning("Warnings in processing service request №{Id}: {Warnings}", Model.Id, Model.Warnings);
            }
        }
    }
}