using System.ComponentModel;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Telemart.Client.Extensions;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Service.ServiceRequests.Create
{
    public sealed class RepairPageViewModel :
        WizardPageViewModelBase<CreateServiceRequestModel>,
        ISupportWizardBackCommand,
        ISupportWizardNextCommand
    {
        public RepairPageViewModel()
        {
            HandleLoadedCommand = new AsyncCommand(HandleLoadedAsync);
        }

        #region Commands

        public IAsyncCommand HandleLoadedCommand { get; }

        #endregion

        public bool CanGoBack => !IsLongOperationInProgress;

        public bool CanGoForward { get; } = true;

        public override string Description { get; } = "Выберите тип ремонта";

        public override string Header { get; } = "Ремонт";

        private IWizardService WizardService => GetService<IWizardService>();

        public void OnGoBack(CancelEventArgs e)
        {
            WizardService.NavigateToView<DeclarantPageViewModel>(Model, ((ISupportParentViewModel)this).ParentViewModel);
            e.Cancel = true;
        }

        public void OnGoForward(CancelEventArgs e)
        {
            WizardService.NavigateToView<LogisticsPageViewModel>(Model, ((ISupportParentViewModel)this).ParentViewModel);
            e.Cancel = true;
        }

        private Task HandleLoadedAsync()
        {
            Model.CalculateRepairsEnabled();

            return Task.CompletedTask;
        }
    }
}