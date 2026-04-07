using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Validation;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.Requests.Features.ServiceProduct.Actions;
using Telemart.Client.Data.Requests.Features.ServiceRequest;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.ServiceProduct;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Service.ServiceProducts
{
    public sealed class ServiceProductSupplierRemoveFromRegisterViewModel : TelemartDialogViewModelBase
    {
        private int serviceProductId;

        public ServiceProductSupplierRemoveFromRegisterViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
        }

        public ServiceProductSupplierRemoveFromRegisterViewModel()
        {
        }

        #region INPC

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

        public static void BuildMetadata(MetadataBuilder<ServiceProductSupplierRemoveFromRegisterViewModel> builder)
        {
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
                    Currency = Currency.GetById(serviceProduct.PurchasedCurrencyId ?? 0);
                    Amount = serviceProduct.PurchasedPrice;
                }
            }

            Title = "Списание товара";
        }

        protected override async Task HandleOkAsync()
        {
            if (IDataErrorInfoHelper.HasErrors(this))
            {
                return;
            }

            try
            {
                SupplierRemoveFromRegisterServiceProduct gatewayRequest = new SupplierRemoveFromRegisterServiceProduct(
                    serviceProductId,
                    Amount.Value,
                    Currency.Id,
                    Date.Value,
                    Comment);

                Result<ServiceProductDto> result = await WebClient.ExecuteApiRequestAsync(gatewayRequest);

                if (result.Warnings.Any())
                {
                    MessageFacadeService.ShowNotificationWarning($"Серв. товар №{result.Data.Id} списан с предупреждениями");

                    ShowValidationResultView(
                        "Предупрежедения",
                        result.Warnings.Select(x => new ValidationResultItem(x, false)).ToList());
                }
                else
                {
                    MessageFacadeService.ShowNotificationInfo($"Серв. товар №{result.Data.Id} успешно списан");
                }

                IsOk = true;
                Close();
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при списании товара");
                ShowValidationResultView("Ошибки при списании товара", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to remove service product from register");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при списании товара");
                Logger.LogError(exception, "Failed to remove service product from register");
            }
        }
    }
}