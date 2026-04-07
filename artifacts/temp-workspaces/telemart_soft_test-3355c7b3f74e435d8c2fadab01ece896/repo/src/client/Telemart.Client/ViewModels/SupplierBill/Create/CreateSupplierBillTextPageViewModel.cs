using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Mvvm.UI;
using Microsoft.Extensions.Logging;
using Telemart.Client.Business.PriceConversion;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Invoice.Actions;
using Telemart.Client.Data.Requests.Features.SupplierBill.Actions;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.SupplierBill;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.SupplierBill.Create.Parsing;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;
using Telemart.Common.PriceConversion;

namespace Telemart.Client.ViewModels.SupplierBill.Create
{
    public sealed class CreateSupplierBillTextPageViewModel :
        WizardPageViewModelBase<CreateSupplierBillModel>,
        ISupportWizardNextCommand,
        ISupportWizardBackCommand
    {
        private readonly IWebClient webClient;
        private readonly IDictionaries dictionaries;
        private readonly IPriceConverterFactory priceConverterFactory;
        private readonly IMessageFacadeService messageFacadeService;

        public CreateSupplierBillTextPageViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IPriceConverterFactory priceConverterFactory,
            IMessageFacadeService messageFacadeService,
            ILogger<CreateSupplierBillTextPageViewModel> logger)
        {
            this.webClient = webClient;
            this.dictionaries = dictionaries;
            this.priceConverterFactory = priceConverterFactory;
            this.messageFacadeService = messageFacadeService;

            HandleLoadedCommand = new AsyncCommand(HandleLoadedAsync);
            RemoveColumnItemCommand = new DelegateCommand<ComboBoxItem>(RemoveColumnItem);

            Logger = logger;
        }

        public CreateSupplierBillTextPageViewModel()
        {
            Model = new CreateSupplierBillModel();
        }

        #region Commands

        public IAsyncCommand HandleLoadedCommand { get; }

        public IDelegateCommand RemoveColumnItemCommand { get; }

        #endregion

        public override string Description { get; } = "Заполните товарную часть";

        public override string Header { get; } = "Шаг 2";

        public bool CanGoBack => !IsLongOperationInProgress;

        public bool CanGoForward => !IsLongOperationInProgress;

        private string[] ValidatableProperties { get; } =
        {
            nameof(CreateSupplierBillModel.Text),
            nameof(CreateSupplierBillModel.PriceWithTax)
        };

        private IWizardService WizardService => GetService<IWizardService>();

        private IDocumentManagerService SizeableDialogDocumentManagerService => GetService<IDocumentManagerService>("SizeableDialogDocumentManagerService", ServiceSearchMode.PreferParents);

        private CurrentWindowService CurrentWindowService => (CurrentWindowService)GetService<ICurrentWindowService>(ServiceSearchMode.PreferParents);

        private ILogger Logger { get; }

        public void OnGoBack(CancelEventArgs e)
        {
            WizardService.NavigateToView<CreateSupplierBillContractorPageViewModel>(Model, ((ISupportParentViewModel)this).ParentViewModel);
            e.Cancel = true;
        }

        public void OnGoForward(CancelEventArgs e)
        {
            if (!IDataErrorInfoHelper.HasErrors(Model, false, propertyFilter: x => ValidatableProperties.Contains(x.Name)))
            {
                IsLongOperationInProgress = true;
                CurrentWindowService.ActualWindow.Closing += ActualWindowClosing;

                try
                {
                    SupplierBillTextParserSettings settings = new SupplierBillTextParserSettings(
                        Model.ColumnItems.IndexOf(Model.NameColumnItem),
                        Model.ColumnItems.IndexOf(Model.CodeColumnItem),
                        Model.ColumnItems.IndexOf(Model.QuantityColumnItem),
                        Model.ColumnItems.IndexOf(Model.PriceColumnItem),
                        Model.ColumnItems.IndexOf(Model.TnvedColumnItem),
                        Model.PriceWithTax.Value);

                    SupplierBillTextParserResult parserResult = SupplierBillTextParser.Parse(Model.Text, settings);

                    if (parserResult.IsOk)
                    {
                        RecognizeSupplierBill request = new RecognizeSupplierBill(
                            Model.Contractor.Id,
                            parserResult.Items.Select(x =>
                                    new SupplierBillRecognizeDto { SupplierName = x.Name, SupplierCode = x.Code })
                                .ToArray());

                        webClient.ExecuteApiRequestAsync(request).ContinueWith(
                            t =>
                            {
                                if (t.IsFaulted && t.Exception != null)
                                {
                                    throw t.Exception;
                                }

                                if (t.Status == TaskStatus.RanToCompletion)
                                {
                                    foreach (CreateSupplierBillProductModel item in parserResult.Items)
                                    {
                                        foreach (SupplierBillRecognizeResultDto recognizeItem in t.Result.Data)
                                        {
                                            if (string.Equals(item.Code, recognizeItem.SupplierCode, StringComparison.OrdinalIgnoreCase))
                                            {
                                                TaxRate taxRate = dictionaries.GetItemById<TaxRate>(recognizeItem.ProductTaxRateId);

                                                item.ProductId = recognizeItem.ProductId;
                                                item.ProductName = recognizeItem.ProductName;
                                                item.TaxRate = taxRate;
                                                item.TaxRateDefault = taxRate;

                                                break;
                                            }
                                        }

                                        if (item.ProductId == null)
                                        {
                                            foreach (SupplierBillRecognizeResultDto recognizeItem in t.Result.Data)
                                            {
                                                if (string.Equals(item.Name, recognizeItem.SupplierName, StringComparison.OrdinalIgnoreCase))
                                                {
                                                    TaxRate taxRate = dictionaries.GetItemById<TaxRate>(recognizeItem.ProductTaxRateId);

                                                    item.ProductId = recognizeItem.ProductId;
                                                    item.ProductName = recognizeItem.ProductName;
                                                    item.TaxRate = taxRate;
                                                    item.TaxRateDefault = taxRate;

                                                    break;
                                                }
                                            }
                                        }
                                    }

                                    Model.SetProducts(parserResult.Items);

                                    WizardService.NavigateToView<CreateSupplierBillMatchingPageViewModel>(Model, ((ISupportParentViewModel)this).ParentViewModel);

                                    IsLongOperationInProgress = false;
                                    CurrentWindowService.ActualWindow.Closing -= ActualWindowClosing;
                                }
                            },
                            TaskScheduler.FromCurrentSynchronizationContext());
                    }
                    else
                    {
                        ShowValidationResultView("Ошибки", parserResult.Errors);
                        IsLongOperationInProgress = false;
                        CurrentWindowService.ActualWindow.Closing -= ActualWindowClosing;
                    }
                }
                catch (UnexpectedSatusException exception) when (exception.Args.HttpStatusCode == HttpStatusCode.Unauthorized)
                {
                    messageFacadeService.ShowNotificationError("Не хватает прав для выполнения операции");
                    IsLongOperationInProgress = false;
                    CurrentWindowService.ActualWindow.Closing -= ActualWindowClosing;

                    Logger.LogError(exception, "Не хватает прав для выполнения операции.");
                }
                catch (Exception ex)
                {
                    messageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
                    IsLongOperationInProgress = false;
                    CurrentWindowService.ActualWindow.Closing -= ActualWindowClosing;

                    Logger.LogError(ex, Resources.ErrorDuringDataLoading);
                }
            }

            e.Cancel = true;
        }

        private void ActualWindowClosing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
        }

        private async Task HandleLoadedAsync()
        {
            Result<InvoiceDto> invoiceResult = await webClient.ExecuteApiRequestAsync(new SetInvoiceReturnedProducts(Model.InvoiceId!.Value));

            ConversionRate[] invoiceConversionRates = invoiceResult.Data.CurrencyRates
                .Select(x => x.CreateConversionRate())
                    .ToArray();

            Model.PriceConverter = await priceConverterFactory.CreateForInvoiceAsync(invoiceResult.Data.SupplierId, invoiceConversionRates);

            Model.InvoiceDto = invoiceResult.Data;

            Model.Currencies ??= dictionaries.GetItems<Currency>()
                .Where(x => x.Id == Currency.UahId || invoiceResult.Data.CurrencyRates.Any(z => z.FromCurrencyId == x.Id))
                .ToReadOnlyObservableCollection();
        }

        private void RemoveColumnItem(ComboBoxItem item)
        {
            if (messageFacadeService.Confirm($"Вы действительно хотите удалить колонку \"{item.DisplayValue}\""))
            {
                Model.ColumnItems.Remove(item);
            }
        }

        private void ShowValidationResultView(string title, IReadOnlyCollection<ValidationResultItem> validationItems)
        {
            SizeableDialogDocumentManagerService.ShowView<ValidationResultViewModel>(
                new ValidationResultViewModelParameter(title, validationItems),
                this);
        }
    }
}