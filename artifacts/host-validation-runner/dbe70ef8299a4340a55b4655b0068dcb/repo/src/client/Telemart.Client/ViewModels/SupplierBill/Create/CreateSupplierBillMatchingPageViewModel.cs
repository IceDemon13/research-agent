using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Nomenclature;
using Telemart.Client.ViewModels.Validation;

namespace Telemart.Client.ViewModels.SupplierBill.Create
{
    public sealed class CreateSupplierBillMatchingPageViewModel :
        WizardPageViewModelBase<CreateSupplierBillModel>,
        ISupportWizardNextCommand,
        ISupportWizardBackCommand
    {
        private readonly IDictionaries _dictionaries;
        private readonly IMessageFacadeService _messageFacadeService;
        private readonly ILogger<CreateSupplierBillMatchingPageViewModel> _logger;

        public CreateSupplierBillMatchingPageViewModel(
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            ILogger<CreateSupplierBillMatchingPageViewModel> logger)
        {
            _dictionaries = dictionaries;
            _messageFacadeService = messageFacadeService;
            _logger = logger;

            HandleLoadedCommand = new AsyncCommand(HandleLoadedAsync);
            SelectProductCommand = new DelegateCommand<CreateSupplierBillProductModel>(SelectProduct, x => x != null);
        }

        #region Commands

        public IAsyncCommand HandleLoadedCommand { get; }

        public IDelegateCommand SelectProductCommand { get; }

        #endregion

        public override string Description { get; } = "Сопоставление";

        public override string Header { get; } = "Шаг 3";

        public bool CanGoBack { get; } = true;

        public bool CanGoForward => true;

        private string[] ValidatableProperties { get; } =
        {
            nameof(CreateSupplierBillModel.Products)
        };

        private IWizardService WizardService => GetService<IWizardService>();

        private IDocumentManagerService SizeableDialogDocumentManagerService => GetService<IDocumentManagerService>("SizeableDialogDocumentManagerService", ServiceSearchMode.PreferParents);

        public void OnGoBack(CancelEventArgs e)
        {
            WizardService.NavigateToView<CreateSupplierBillTextPageViewModel>(Model, ((ISupportParentViewModel)this).ParentViewModel);
            e.Cancel = true;
        }

        public void OnGoForward(CancelEventArgs e)
        {
            try
            {
                if (!IDataErrorInfoHelper.HasErrors(Model, false, propertyFilter: x => ValidatableProperties.Contains(x.Name)))
                {
                    List<CreateSupplierBillProductModel> products = Model.Products
                        .Where(x => !Model.InvoiceDto.InvoiceProducts
                            .Any(y => y.ProductId == x.ProductId
                                      && x.Quantity <= (y.QuantityReal - y.QuantityReturned - y.BillsQuantity)))
                        .ToList();

                    if (products.Any())
                    {
                        ShowValidationResultView(
                            "Ошибки валидации товаров",
                            products.Select(x => new ValidationResultItem($"Товара \"{x.Name}\" нет в достаточном количестве в накладной", true)).ToArray());
                        return;
                    }

                    foreach (CreateSupplierBillProductModel modelProduct in Model.Products)
                    {
                        InvoiceProductDto invoiceProduct = Model.InvoiceDto.InvoiceProducts.FirstOrDefault(x => x.ProductId == modelProduct.ProductId);

                        modelProduct.SetInvoiceProduct(invoiceProduct);
                    }

                    Model.SetSummaryItems();
                    WizardService.NavigateToView<CreateSupplierBillConfirmPageViewModel>(Model, ((ISupportParentViewModel)this).ParentViewModel);
                }
            }
            catch (UnexpectedSatusException exception)
            {
                _messageFacadeService.ShowNotificationError("Ошибка при запросе накладной");
                ShowValidationResultView("Ошибки при запросе накладной", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                _logger.LogError(exception, "Failed to query invoice");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                _messageFacadeService.ShowNotificationError("Непредвиденная ошибка");
                _logger.LogError(exception, "Error while go forward on supplier bill page");
            }

            e.Cancel = true;
        }

        private bool ShowValidationResultView(string title, IEnumerable<ValidationResultItem> validationItems)
        {
            ValidationResultViewModel viewModel = SizeableDialogDocumentManagerService.ShowView<ValidationResultViewModel>(
                new ValidationResultViewModelParameter(title, validationItems),
                this);

            return viewModel.IsOk;
        }

        private Task HandleLoadedAsync()
        {
            return Task.CompletedTask;
        }

        private void SelectProduct(CreateSupplierBillProductModel item)
        {
            NomenclatureViewOptions options = new NomenclatureViewOptions(
                NomenclatureViewPriceContext.Client,
                Constants.TelemartContractorId,
                NomenclatureViewSelectionMode.Single,
                false,
                searchText: item.Name);

            NomenclatureViewModel nomenclatureViewModel = SizeableDialogDocumentManagerService.ShowView<NomenclatureViewModel>(options, this);

            if (nomenclatureViewModel.IsOk)
            {
                NomenclatureViewItem nomenclatureViewItem = nomenclatureViewModel.GetSelectedItems().First();

                TaxRate taxRate = _dictionaries.GetItemById<TaxRate>(nomenclatureViewItem.TaxRateId);

                item.ProductId = nomenclatureViewItem.Id;
                item.ProductName = nomenclatureViewItem.Name;
                item.TaxRate = taxRate;
                item.TaxRateDefault = taxRate;

                Model.CurrencyIdChanged();
            }
        }
    }
}