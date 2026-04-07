using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using Telemart.Client.Common;
using Telemart.Client.Extensions;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Nomenclature;

namespace Telemart.Client.ViewModels.Service.ServiceRequests.Create
{
    public sealed class CreateManyServiceRequestStep4ChangeViewModel :
        WizardPageViewModelBase<CreateManyServiceRequestModel>, ISupportWizardNextCommand, ISupportWizardBackCommand
    {
        public CreateManyServiceRequestStep4ChangeViewModel()
        {
            HandleLoadedCommand = new AsyncCommand(HandleLoadedAsync);
            SelectProductCommand = new DelegateCommand<int>(SelectProduct);
            RemoveProductCommand = new DelegateCommand<int>(RemoveProduct);
        }

        public override string Description => "На какой товар клиент хочет произвести обмен?";

        public override string Header => "Обмен";

        public bool CanGoForward { get; } = true;

        public bool CanGoBack => !IsLongOperationInProgress;

        public IAsyncCommand HandleLoadedCommand { get; }

        public IDelegateCommand RemoveProductCommand { get; }

        public IDelegateCommand SelectProductCommand { get; }

        private IWizardService WizardService => GetService<IWizardService>();

        private IDocumentManagerService SizeableDialogDocumentManagerService => GetService<IDocumentManagerService>(
            "SizeableDialogDocumentManagerService",
            ServiceSearchMode.PreferParents);

        public void OnGoBack(CancelEventArgs e)
        {
            WizardService.NavigateToView<CreateManyServiceRequestStep3ViewModel>(
                Model,
                ((ISupportParentViewModel) this).ParentViewModel);
            e.Cancel = true;
        }

        public void OnGoForward(CancelEventArgs e)
        {
            WizardService.NavigateToView<CreateManyServiceRequestStep5ViewModel>(
                Model,
                ((ISupportParentViewModel) this).ParentViewModel);
            e.Cancel = true;
        }

        private Task HandleLoadedAsync()
        {
            foreach (ServiceRequestProductSnViewItem selectedProduct in Model.SelectedProducts)
            {
                selectedProduct.ChangeOnProductId = selectedProduct.ChangeOnProductId;
                selectedProduct.ChangeOnProductName = selectedProduct.Name;
            }

            return Task.CompletedTask;
        }

        private void RemoveProduct(int changeOnProductId)
        {
            ServiceRequestProductSnViewItem selectedProduct =
                Model.SelectedProducts.First(x => x.ChangeOnProductId == changeOnProductId);

            selectedProduct.ChangeOnProductId = null;
            selectedProduct.ChangeOnProductName = null;
        }

        private void SelectProduct(int productId)
        {
            NomenclatureViewOptions options = new NomenclatureViewOptions(
                NomenclatureViewPriceContext.Client,
                Constants.TelemartContractorId,
                NomenclatureViewSelectionMode.Single,
                false);

            NomenclatureViewModel nomenclatureViewModel =
                SizeableDialogDocumentManagerService.ShowView<NomenclatureViewModel>(options, this);

            if (nomenclatureViewModel.IsOk)
            {
                NomenclatureViewItem product = nomenclatureViewModel.GetSelectedItems().First();

                ServiceRequestProductSnViewItem selectedProduct =
                    Model.SelectedProducts.First(x => x.ProductId == productId);

                selectedProduct.ChangeOnProductId = product.Id;
                selectedProduct.ChangeOnProductName = product.NameFull;
            }
        }
    }
}