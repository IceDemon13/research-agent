using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.SupplierBill;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects.SupplierBill;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.SupplierBill.Create
{
    public sealed class CreateSupplierBillConfirmPageViewModel :
        WizardPageViewModelBase<CreateSupplierBillModel>,
        ISupportWizardNextCommand,
        ISupportWizardBackCommand
    {
        private readonly IWebClient _webClient;
        private readonly IDictionaries _dictionaries;
        private readonly IMessageFacadeService _messageFacadeService;
        private readonly ILogger<CreateSupplierBillConfirmPageViewModel> _logger;

        public CreateSupplierBillConfirmPageViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            ILogger<CreateSupplierBillConfirmPageViewModel> logger)
        {
            _webClient = webClient;
            _dictionaries = dictionaries;
            _messageFacadeService = messageFacadeService;
            _logger = logger;

            HandleLoadedCommand = new AsyncCommand(HandleLoadedAsync);
            TaxRateChangedCommand = new DelegateCommand(() => Model?.SetSummaryItems());
        }

        public CreateSupplierBillConfirmPageViewModel()
        {
        }

        #region Commands

        public IAsyncCommand HandleLoadedCommand { get; }

        public IDelegateCommand TaxRateChangedCommand { get; }

        #endregion

        public ReadOnlyObservableCollection<TaxRate> TaxRates
        {
            get { return GetProperty(() => TaxRates); }
            private set { SetProperty(() => TaxRates, value); }
        }

        public PriceEpsilonsDto PriceEpsilons
        {
            get { return GetProperty(() => PriceEpsilons); }
            set { SetProperty(() => PriceEpsilons, value); }
        }

        public override string Description { get; } = "Подтверждение";

        public override string Header { get; } = "Шаг 4";

        public bool CanGoBack { get; } = true;

        public bool CanGoForward => true;

        private string[] ValidatableProperties { get; } = Array.Empty<string>();

        private IWizardService WizardService => GetService<IWizardService>();

        private IDocumentManagerService SizeableDialogDocumentManagerService => GetService<IDocumentManagerService>("SizeableDialogDocumentManagerService", ServiceSearchMode.PreferParents);

        public void OnGoBack(CancelEventArgs e)
        {
            WizardService.NavigateToView<CreateSupplierBillMatchingPageViewModel>(Model, ((ISupportParentViewModel)this).ParentViewModel);
            e.Cancel = true;
        }

        public void OnGoForward(CancelEventArgs e)
        {
            try
            {
                if (IDataErrorInfoHelper.HasErrors(Model, false, propertyFilter: x => ValidatableProperties.Contains(x.Name)))
                {
                    return;
                }

                IReadOnlyCollection<ValidationResultItem> errors = Model.Products
                    .SelectMany(x =>
                        x.ValidateInvoiceProduct(
                            Model.Contractor.Discount,
                            Model.CurrencyId!.Value,
                            PriceEpsilons,
                            Model.PriceConverter))
                    .Select(x => new ValidationResultItem(x, true))
                    .ToArray();

                if (errors.Any())
                {
                    ShowValidationResultView("Ошибки валидации товаров", errors);
                    return;
                }

                IsLongOperationInProgress = true;

                SupplierBillCreateDto dto = new SupplierBillCreateDto
                {
                    SupplierId = Model.Contractor.Id,
                    Edrpou = Model.Contractor.Edrpou,
                    InvoicedOn = Model.Date.Value,
                    InvoiceId = Model.InvoiceId.Value,
                    CurrencyId = Model.CurrencyId.Value,
                    Number = Model.Number,
                    Products = Model.Products
                        .Select(x => new SupplierBillProductCreateDto
                        {
                            ProductId = x.ProductId.Value,
                            InvoiceProductId = x.InvoiceProductId.Value,
                            Quantity = x.Quantity,
                            TaxRateId = x.TaxRate.Id,
                            Price = x.PriceNoTax,
                            PriceTax = x.PriceWithTax,
                            Sum = x.SumNoTax,
                            SumTax = x.SumWithTax,
                            Tnved = x.Tnved
                        })
                        .ToArray()
                };

                Task<Result<SupplierBillDto>> task = _webClient.ExecuteApiRequestAsync(new CreateSupplierBill(dto));

                task.ContinueWith(
                    t =>
                    {
                        if (t.IsFaulted)
                        {
                            Exception exception = t.Exception?.Flatten().InnerException;
                            Model.ValidationItems = GetValidationItemsFromException(exception).ToObservableCollection();
                        }

                        if (t.Status == TaskStatus.RanToCompletion)
                        {
                            Model.Result = t.Result.Data;

                            if (t.Result.Warnings?.Any() == true)
                            {
                                _messageFacadeService.ShowValidationResultView("Предупреждения", t.Result.Warnings.Select(x => new ValidationResultItem(x, false)), this);
                            }
                        }

                        WizardService.NavigateToView<CreateSupplierBillFinishPageViewModel>(Model, ((ISupportParentViewModel)this).ParentViewModel);

                        IsLongOperationInProgress = false;
                    },
                    TaskScheduler.FromCurrentSynchronizationContext());
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to create supplier bill");
                _messageFacadeService.ShowNotificationError("Ошибка при создании заявки");
                IsLongOperationInProgress = false;
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

        private async Task HandleLoadedAsync()
        {
            TaxRates = _dictionaries.GetItems<TaxRate>().ToReadOnlyObservableCollection();

            PriceEpsilons = await _webClient.ExecuteApiRequestAsync(new QuerySupplierBillPriceEpsilons());
        }
    }
}