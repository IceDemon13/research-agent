using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.Messages;
using Telemart.Client.Extensions;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.SupplierBill.Create
{
    public sealed class CreateSupplierBillFinishPageViewModel :
        WizardPageViewModelBase<CreateSupplierBillModel>,
        ISupportWizardBackCommand,
        ISupportWizardFinishCommand
    {
        public CreateSupplierBillFinishPageViewModel(IMessenger messenger)
        {
            Messenger = messenger;

            HandleLoadedCommand = new AsyncCommand(HandleLoadedAsync);
        }

        public CreateSupplierBillFinishPageViewModel()
        {
        }

        #region Commands

        public IAsyncCommand HandleLoadedCommand { get; }

        #endregion

        public bool CanFinish => true;

        public bool CanGoBack => Model.ValidationItems?.Any(x => x.IsError) == true;

        public override string Description => Model.ValidationItems != null && Model.ValidationItems.Any()
            ? "Ошибки при создании счета поставщика"
            : $"Счет поставщика №{Model.Result.Id} успешно создан";

        public override string Header { get; } = "Результат";

        private IWizardService WizardService => GetService<IWizardService>();

        private IMessenger Messenger { get; }

        public void OnFinish(CancelEventArgs e)
        {
            if (Model.Result != null && Model.OpenAfterCreation)
            {
                Messenger.Send(new SupplierBillViewMessage(Model.Result.Id));
            }
        }

        public void OnGoBack(CancelEventArgs e)
        {
            Model.ValidationItems = null;
            Model.Result = null;

            WizardService.NavigateToView<CreateSupplierBillConfirmPageViewModel>(Model, ((ISupportParentViewModel)this).ParentViewModel);
            e.Cancel = true;
        }

        private Task HandleLoadedAsync()
        {
            if (Model.Result != null)
            {
                Messenger.Send(new SupplierBillMessage(Model.Result, MessageType.Added));
            }

            return Task.CompletedTask;
        }
    }
}