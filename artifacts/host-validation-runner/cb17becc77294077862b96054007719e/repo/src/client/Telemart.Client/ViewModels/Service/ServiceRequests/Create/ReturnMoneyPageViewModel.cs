using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using Telemart.Client.Extensions;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Service.ServiceRequests.Create
{
    public sealed class ReturnMoneyPageViewModel :
        WizardPageViewModelBase<CreateServiceRequestModel>,
        ISupportWizardBackCommand,
        ISupportWizardNextCommand
    {
        public ReturnMoneyPageViewModel()
        {
            HandleLoadedCommand = new AsyncCommand(() => Task.CompletedTask);
        }

        public IAsyncCommand HandleLoadedCommand { get; }

        public bool CanGoBack => !IsLongOperationInProgress;

        public bool CanGoForward { get; } = true;

        public override string Description { get; } = "Выберите способ возврата ДС";

        public override string Header { get; } = "Возврат денежных средств";

        private IWizardService WizardService => GetService<IWizardService>();

        private string[] ValidatableProperties { get; } =
        {
            nameof(CreateServiceRequestModel.ReturnMoneyPaymentType),
            nameof(CreateServiceRequestModel.Cashbox),
            nameof(CreateServiceRequestModel.Requisites)
        };

        public void OnGoBack(CancelEventArgs e)
        {
            WizardService.NavigateToView<DeclarantPageViewModel>(Model, ((ISupportParentViewModel)this).ParentViewModel);
            e.Cancel = true;
        }

        public void OnGoForward(CancelEventArgs e)
        {
            if (!IDataErrorInfoHelper.HasErrors(Model, false, propertyFilter: x => ValidatableProperties.Contains(x.Name)))
            {
                WizardService.NavigateToView<LogisticsPageViewModel>(Model, ((ISupportParentViewModel)this).ParentViewModel);
            }

            e.Cancel = true;
        }
    }
}