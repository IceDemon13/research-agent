using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common;
using Telemart.Client.Extensions;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Nomenclature;

namespace Telemart.Client.ViewModels.Service.ServiceRequests.Create
{
    public sealed class ChangePageViewModel :
        WizardPageViewModelBase<CreateServiceRequestModel>,
        ISupportWizardBackCommand,
        ISupportWizardNextCommand
    {
        public ChangePageViewModel()
        {
            HandleLoadedCommand = new AsyncCommand(HandleLoadedAsync);
            SelectProductCommand = new DelegateCommand(SelectProduct);
            RemoveProductCommand = new DelegateCommand(RemoveProduct);
        }

        #region Commands

        public IAsyncCommand HandleLoadedCommand { get; }

        public IDelegateCommand RemoveProductCommand { get; }

        public IDelegateCommand SelectProductCommand { get; }

        #endregion

        public bool CanGoBack => !IsLongOperationInProgress;

        public bool CanGoForward { get; } = true;

        public override string Description { get; } = "На какой товар клиент хочет произвести обмен?";

        public override string Header { get; } = "Обмен";

        private IDocumentManagerService SizeableDialogDocumentManagerService => GetService<IDocumentManagerService>("SizeableDialogDocumentManagerService", ServiceSearchMode.PreferParents);

        private IWizardService WizardService => GetService<IWizardService>();

        private string[] ValidatableProperties { get; } =
        {
            nameof(CreateServiceRequestModel.ChangeOnProductName)
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

        private Task HandleLoadedAsync()
        {
            int productId = Model.ProductId.Value;
            string productName = Model.Products.First(x => x.Id == productId).DisplayValue;

            Model.ChangeOnProductId = productId;
            Model.ChangeOnProductName = productName;

            return Task.CompletedTask;
        }

        private void RemoveProduct()
        {
            Model.ChangeOnProductId = null;
            Model.ChangeOnProductName = null;
        }

        private void SelectProduct()
        {
            NomenclatureViewOptions options = new NomenclatureViewOptions(
                NomenclatureViewPriceContext.Client,
                Constants.TelemartContractorId,
                NomenclatureViewSelectionMode.Single,
                false);

            NomenclatureViewModel nomenclatureViewModel = SizeableDialogDocumentManagerService.ShowView<NomenclatureViewModel>(options, this);

            if (nomenclatureViewModel.IsOk)
            {
                NomenclatureViewItem product = nomenclatureViewModel.GetSelectedItems().First();

                Model.ChangeOnProductId = product.Id;
                Model.ChangeOnProductName = product.Name;
            }
        }
    }
}