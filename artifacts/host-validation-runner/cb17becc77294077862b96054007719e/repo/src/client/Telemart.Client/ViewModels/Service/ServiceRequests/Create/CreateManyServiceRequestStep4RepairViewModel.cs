using System.ComponentModel;
using DevExpress.Mvvm;
using Telemart.Client.Extensions;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Service.ServiceRequests.Create
{
    public sealed class CreateManyServiceRequestStep4RepairViewModel : WizardPageViewModelBase<CreateManyServiceRequestModel>, ISupportWizardNextCommand, ISupportWizardBackCommand
    {
        public bool CanGoForward { get; } = true;

        public bool CanGoBack { get; } = true;

        public override string Description => "Укажите тип ремонта";

        public override string Header => "Тип ремонта";

        private IWizardService WizardService => GetService<IWizardService>();

        public void OnGoBack(CancelEventArgs e)
        {
            WizardService.NavigateToView<CreateManyServiceRequestStep3ViewModel>(Model, ((ISupportParentViewModel)this).ParentViewModel);
            e.Cancel = true;
        }

        public void OnGoForward(CancelEventArgs e)
        {
            WizardService.NavigateToView<CreateManyServiceRequestStep5ViewModel>(Model, ((ISupportParentViewModel)this).ParentViewModel);

            e.Cancel = true;
        }
    }
}
