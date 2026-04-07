using System.ComponentModel;
using DevExpress.Mvvm;
using Telemart.Client.Extensions;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Service.ServiceRequests.Create
{
    public sealed class CreateManyServiceRequestStep4ReturnViewModel : WizardPageViewModelBase<CreateManyServiceRequestModel>, ISupportWizardNextCommand, ISupportWizardBackCommand
    {
        public CreateManyServiceRequestStep4ReturnViewModel()
        {
            HandlePaymentTypeChangedCommand = new DelegateCommand(HandlePaymentTypeChanged);
        }

        public IAsyncCommand HandleLoadedCommand { get; }

        public IDelegateCommand HandlePaymentTypeChangedCommand { get; }

        public bool CanGoForward { get; } = true;

        public bool CanGoBack => !IsLongOperationInProgress;

        public override string Description => "Выберите способ возврата ДС";

        public override string Header => "Возврат денежных средств";

        private IWizardService WizardService => GetService<IWizardService>();

        public void OnGoForward(CancelEventArgs e)
        {
            WizardService.NavigateToView<CreateManyServiceRequestStep5ViewModel>(Model, ((ISupportParentViewModel)this).ParentViewModel);
            e.Cancel = true;
        }

        public void OnGoBack(CancelEventArgs e)
        {
            WizardService.NavigateToView<CreateManyServiceRequestStep3ViewModel>(Model, ((ISupportParentViewModel)this).ParentViewModel);
            e.Cancel = true;
        }

        private void HandlePaymentTypeChanged()
        {
            Model.ReturnMoneyPaymentTypeChanged();
        }
    }
}