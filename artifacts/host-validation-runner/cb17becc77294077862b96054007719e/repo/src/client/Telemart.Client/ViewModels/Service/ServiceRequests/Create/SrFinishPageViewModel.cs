using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.Messages;
using Telemart.Client.Extensions;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Service.ServiceRequests.Create
{
    public sealed class SrFinishPageViewModel :
        WizardPageViewModelBase<CreateServiceRequestModel>,
        ISupportWizardBackCommand,
        ISupportWizardFinishCommand
    {
        public SrFinishPageViewModel(IMessenger messenger)
        {
            Messenger = messenger;

            HandleLoadedCommand = new AsyncCommand(HandleLoadedAsync);
        }

        public SrFinishPageViewModel()
        {
        }

        #region Commands

        public IAsyncCommand HandleLoadedCommand { get; }

        #endregion

        public bool CanFinish => true;

        public bool CanGoBack => Model.ValidationItems?.Any(x => x.IsError) == true;

        public override string Description => Model.ValidationItems != null && Model.ValidationItems.Any()
            ? "Ошибки при создании заявки"
            : $"Заявка №{Model.Result.Id} успешно создана";

        public override string Header { get; } = "Результат";

        private IWizardService WizardService => GetService<IWizardService>();

        private IMessenger Messenger { get; }

        public void OnFinish(CancelEventArgs e)
        {
        }

        public void OnGoBack(CancelEventArgs e)
        {
            Model.ValidationItems = null;
            Model.Result = null;

            WizardService.NavigateToView<DeclarantPageViewModel>(Model, ((ISupportParentViewModel)this).ParentViewModel);
            e.Cancel = true;
        }

        private Task HandleLoadedAsync()
        {
            if (Model.Result != null)
            {
                Messenger.Send(new ServiceRequestMessage(Model.Result, MessageType.Added));
            }

            return Task.CompletedTask;
        }
    }
}