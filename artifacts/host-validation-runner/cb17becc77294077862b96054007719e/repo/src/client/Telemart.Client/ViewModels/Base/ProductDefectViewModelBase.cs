using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Validation;
using Telemart.Client.Views.Common.Product;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Base
{
    [ViewName(nameof(ProductDefectView))]
    public abstract class ProductDefectViewModelBase<TResult, TBody> : TelemartDialogViewModelBase
        where TBody : class
        where TResult : ProductDefectResultDtoBase
    {
        public ProductDefectViewModelBase(
             IWebClient webClient,
             IDictionaries dictionaries,
             IMessageFacadeService messageFacadeService,
             string title)
             : base(webClient, dictionaries, messageFacadeService)
        {
            Title = title;
            IsSelectedProductReadOnly = true;
            IsSerialNumberReadOnly = true;
        }

        public bool IsSerialNumberReadOnly
        {
            get { return GetProperty(() => IsSerialNumberReadOnly); }
            set { SetProperty(() => IsSerialNumberReadOnly, value); }
        }

        public bool IsSelectedProductReadOnly
        {
            get { return GetProperty(() => IsSelectedProductReadOnly); }
            set { SetProperty(() => IsSelectedProductReadOnly, value); }
        }

        public ReadOnlyObservableCollection<ProductComboBoxItem> Products
        {
            get { return GetProperty(() => Products); }
            private set { SetProperty(() => Products, value); }
        }

        public ProductComboBoxItem? SelectedProduct
        {
            get { return GetProperty(() => SelectedProduct); }
            set { SetProperty(() => SelectedProduct, value, () => RaisePropertyChanged(nameof(SerialNumber))); }
        }

        public string SerialNumber
        {
            get { return GetProperty(() => SerialNumber); }
            set { SetProperty(() => SerialNumber, value); }
        }

        public string StatedDefect
        {
            get { return GetProperty(() => StatedDefect); }
            set { SetProperty(() => StatedDefect, value); }
        }

        public bool CreateDiscount
        {
            get { return GetProperty(() => CreateDiscount); }
            set { SetProperty(() => CreateDiscount, value); }
        }

        public bool VisibleCreateDiscount
        {
            get { return GetProperty(() => VisibleCreateDiscount); }
            set { SetProperty(() => VisibleCreateDiscount, value); }
        }

        public static void BuildMetadata(MetadataBuilder<ProductDefectViewModelBase<TResult, TBody>> builder)
        {
            builder.Property(x => x.SerialNumber)
              .MatchesInstanceRule((x, y) => y.SelectedProduct?.KeepSerial != true || x != null, () => Resources.RequiredErrorMessage);

            builder.Property(x => x.SelectedProduct).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.StatedDefect).Required(() => Resources.RequiredErrorMessage);
        }

        protected abstract CallEntityActionWithBodyRequestResultBase<TResult, TBody> GetDefectRequest();

        protected virtual IEnumerable<ProductComboBoxItem> GetProducts()
        {
            return new List<ProductComboBoxItem>() { SelectedProduct.Value };
        }

        protected virtual ProductComboBoxItem? GetSelectedProduct()
        {
            IsSelectedProductReadOnly = false;
            return null;
        }

        protected virtual string GetSerialNumber()
        {
            IsSerialNumberReadOnly = false;
            return null;
        }

        protected virtual Task<bool> BeforeProcessOkAsync()
        {
            return Task.FromResult(true);
        }

        protected virtual async Task AfterSuccessOkAsync(TResult result)
        {
        }

        protected override Task HandleLoadedAsync()
        {
            SelectedProduct = GetSelectedProduct();
            Products = GetProducts()?.ToReadOnlyObservableCollection();
            SerialNumber = GetSerialNumber();

            return base.HandleLoadedAsync();
        }

        protected override async Task HandleOkAsync()
        {
            try
            {
                bool beforeProcessOk = await BeforeProcessOkAsync();

                if (!beforeProcessOk)
                {
                    return;
                }

                CallEntityActionWithBodyRequestResultBase<TResult, TBody> request = GetDefectRequest();

                Result<TResult> result = await WebClient.ExecuteApiRequestAsync(request);

                if (result.Warnings.Any())
                {
                    IReadOnlyCollection<ValidationResultItem> validationResultItems = result
                        .Warnings
                        .Select(x => new ValidationResultItem(x, false))
                        .ToList();

                    string message = "Товар обработан с предупреждениями";

                    ShowValidationResultView(message, validationResultItems);

                    MessageFacadeService.ShowNotificationWarning(message);
                }
                else
                {
                    MessageFacadeService.ShowMessageBoxInfo($"Товар успешно обработан. Номер сервисной заявки {result.Data?.ServiceRequest.Id}");
                }

                await AfterSuccessOkAsync(result.Data);

                IsOk = true;
                Close();
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при обработке товара");
                ShowValidationResultView("Ошибки при обработке товара", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to process defect product");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка обработке товара");
                Logger.LogError(exception, "Error while processing defect product");
            }
        }
    }
}