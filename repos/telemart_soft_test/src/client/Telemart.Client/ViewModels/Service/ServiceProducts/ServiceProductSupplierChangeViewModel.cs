using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using Microsoft.Extensions.Logging;
using Telemart.Client.Business.Order;
using Telemart.Client.Common;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Validation;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.Requests.Features.Invoice;
using Telemart.Client.Data.Requests.Features.Products;
using Telemart.Client.Data.Requests.Features.ServiceProduct.Actions;
using Telemart.Client.Data.Requests.Features.ServiceRequest;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.ServiceProduct;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Nomenclature;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Service.ServiceProducts
{
    public sealed class ServiceProductSupplierChangeViewModel : TelemartDialogViewModelBase
    {
        private int serviceProductId;

        public ServiceProductSupplierChangeViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IOrderRules orderRules)
            : base(webClient, dictionaries, messageFacadeService)
        {
            OrderRules = orderRules;
            SelectProductCommand = new DelegateCommand(SelectProduct);
            ClearProductCommand = new DelegateCommand(ClearProduct);
        }

        public ServiceProductSupplierChangeViewModel()
        {
        }

        #region Commands

        public IDelegateCommand SelectProductCommand { get; }

        public IDelegateCommand ClearProductCommand { get; }

        #endregion

        #region INPC

        public ProductItem Product
        {
            get
            {
                return GetProperty(() => Product);
            }

            set
            {
                SetProperty(() => Product, value, ChangedCallback);

                void ChangedCallback()
                {
                    SerialNumber = null;
                    RaisePropertyChanged(nameof(SerialNumber));
                }
            }
        }

        public string SerialNumber
        {
            get { return GetProperty(() => SerialNumber); }
            set { SetProperty(() => SerialNumber, value); }
        }

        public decimal? Amount
        {
            get { return GetProperty(() => Amount); }
            set { SetProperty(() => Amount, value); }
        }

        public Currency Currency
        {
            get { return GetProperty(() => Currency); }
            set { SetProperty(() => Currency, value); }
        }

        public DateTime? Date
        {
            get { return GetProperty(() => Date); }
            set { SetProperty(() => Date, value); }
        }

        public string SupplierName
        {
            get { return GetProperty(() => SupplierName); }
            private set { SetProperty(() => SupplierName, value); }
        }

        public string Comment
        {
            get { return GetProperty(() => Comment); }
            set { SetProperty(() => Comment, value); }
        }

        public DateTime DateMinValue
        {
            get { return GetProperty(() => DateMinValue); }
            private set { SetProperty(() => DateMinValue, value); }
        }

        public DateTime DateMaxValue
        {
            get { return GetProperty(() => DateMaxValue); }
            private set { SetProperty(() => DateMaxValue, value); }
        }

        public ObservableCollection<Currency> Currencies
        {
            get { return GetProperty(() => Currencies); }
            private set { SetProperty(() => Currencies, value); }
        }

        #endregion

        private IOrderRules OrderRules { get; }

        public static void BuildMetadata(MetadataBuilder<ServiceProductSupplierChangeViewModel> builder)
        {
            builder.Property(x => x.Product).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.SerialNumber).MatchesInstanceRule(
                (x, y) => y.Product == null || !y.Product.KeepSerial || !string.IsNullOrWhiteSpace(x),
                () => Resources.RequiredErrorMessage);
            builder.Property(x => x.Amount)
                .Required(() => Resources.RequiredErrorMessage)
                .MatchesRule(x => x >= 0.01m, () => "Цена товара должна быть больше 0")
                .MaxProductPrice();
            builder.Property(x => x.Currency).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.Date).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.SupplierName).Required(() => Resources.RequiredErrorMessage);
        }

        protected override async Task HandleLoadedAsync()
        {
            ServiceProductViewItem serviceProduct = (ServiceProductViewItem)Parameter;

            serviceProductId = serviceProduct.Id;

            Currencies = new ObservableCollection<Currency> { Currency.Uah, Currency.Usd, Currency.Eur };
            Currency = Currency.Uah;
            Amount = 0;

            ServiceRequestDto serviceRequest = await WebClient.ExecuteApiRequestAsync(new QueryServiceRequest(serviceProduct.ServiceRequestId));

            DateMinValue = serviceRequest.CreatedOn;
            DateMaxValue = DateTime.Today;

            if (serviceProduct.SupplierId.HasValue)
            {
                ContractorDto supplier = await WebClient.ExecuteApiRequestAsync(new QueryContractor(serviceProduct.SupplierId.Value));
                SupplierName = supplier.Name;

                if (serviceRequest.InvoiceId.HasValue)
                {
                    InvoiceDto invoice = await WebClient.ExecuteApiRequestAsync(new QueryInvoice(serviceRequest.InvoiceId.Value));

                    if (serviceProduct.SupplierId.Value == invoice.SupplierId)
                    {
                        InvoiceProductDto invoiceProduct = invoice.InvoiceProducts.First(x => x.ProductId == serviceProduct.ProductId);

                        Currency = Currency.GetById(invoiceProduct.CurrencyId);
                        Amount = invoiceProduct.Price;
                    }
                }
            }

            ProductAttributesDto productAttributes = await WebClient.ExecuteApiRequestAsync(new QueryProductAttributes(serviceProduct.ProductId));

            Product = new ProductItem
            {
                Id = productAttributes.ProductId,
                Name = productAttributes.Name,
                KeepSerial = productAttributes.KeepSerial,
                SerialNumberLength = productAttributes.SerialNumberLength
            };

            Title = "Обмен";
        }

        protected override async Task HandleOkAsync()
        {
            if (IDataErrorInfoHelper.HasErrors(this))
            {
                return;
            }

            string errorMessage = ValidateSerialNumber();

            if (!string.IsNullOrEmpty(errorMessage))
            {
                MessageFacadeService.ShowNotificationError(errorMessage, true);
                return;
            }

            try
            {
                SupplierChangeServiceProduct gatewayRequest = new SupplierChangeServiceProduct(
                    serviceProductId,
                    Product.Id,
                    SerialNumber,
                    Amount.Value,
                    Currency.Id,
                    Date.Value,
                    Comment);

                Result<ServiceProductDto> result = await WebClient.ExecuteApiRequestAsync(gatewayRequest);

                if (result.Warnings.Any())
                {
                    MessageFacadeService.ShowNotificationWarning($"Серв. товар №{result.Data.Id} обменян с предупреждениями");

                    ShowValidationResultView(
                        "Предупрежедения",
                        result.Warnings.Select(x => new ValidationResultItem(x, false)).ToList());
                }
                else
                {
                    MessageFacadeService.ShowNotificationInfo($"Серв. товар №{result.Data.Id} успешно обменян");
                }

                IsOk = true;
                Close();
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при обмене товара");
                ShowValidationResultView("Ошибки при обмене товара", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to change service product");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при обмене товара");
                Logger.LogError(exception, "Failed to change service product");
            }
        }

        private void ClearProduct()
        {
            Product = null;
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

                ProductAttributesDto productAttributes = WebClient.ExecuteApiRequest(new QueryProductAttributes(product.Id));

                Product = new ProductItem
                {
                    Id = product.Id,
                    Name = product.Name,
                    KeepSerial = productAttributes.KeepSerial,
                    SerialNumberLength = productAttributes.SerialNumberLength
                };
            }
        }

        private string ValidateSerialNumber()
        {
            string errorMessage = Product.KeepSerial
                ? OrderRules.ValidateSerialNumber(SerialNumber, Product.SerialNumberLength)
                : null;

            return errorMessage;
        }

        public class ProductItem : BindableBase
        {
            public int Id
            {
                get { return GetProperty(() => Id); }
                set { SetProperty(() => Id, value); }
            }

            public string Name
            {
                get { return GetProperty(() => Name); }
                set { SetProperty(() => Name, value); }
            }

            public bool KeepSerial
            {
                get { return GetProperty(() => KeepSerial); }
                set { SetProperty(() => KeepSerial, value, () => { RaisePropertyChanged(nameof(SerialNumber)); }); }
            }

            public List<ProductSnLengthDto> SerialNumberLength
            {
                get { return GetProperty(() => SerialNumberLength); }
                set { SetProperty(() => SerialNumberLength, value); }
            }

            public override string ToString()
            {
                return Name;
            }
        }
    }
}