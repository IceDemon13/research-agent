using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Invoice;
using Telemart.Client.Data.Requests.Features.Invoice.Actions;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Dictionaries.Constants;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Store.Order;
using Telemart.Client.ViewModels.Store.Order.ProductInformation;

namespace Telemart.Client.ViewModels.Store.Invoice
{
    public class InvoiceDelayViewModel : TelemartDialogViewModelBase
    {
        private InvoiceDelayParameter _parameter;

        public InvoiceDelayViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IErrorHandler errorHandler,
            ProductInformationViewModel productInformationViewModel)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Mapper = mapper;
            ErrorHandler = errorHandler;

            ProductInformation = productInformationViewModel;
        }

        public ReadOnlyObservableCollection<ComboBoxItem> ExpireReasons
        {
            get { return GetProperty(() => ExpireReasons); }
            private set { SetProperty(() => ExpireReasons, value); }
        }

        public int? ExpireReasonId
        {
            get { return GetProperty(() => ExpireReasonId); }
            set { SetProperty(() => ExpireReasonId, value); }
        }

        public ReadOnlyObservableCollection<InvoiceDelayProductViewItem> Products
        {
            get { return GetProperty(() => Products); }
            private set { SetProperty(() => Products, value); }
        }

        public InvoiceDelayProductViewItem SelectedProduct
        {
            get { return GetProperty(() => SelectedProduct); }
            set { SetProperty(() => SelectedProduct, value, ProductChanged); }
        }

        public ProductInformationViewModel ProductInformation
        {
            get { return GetProperty(() => ProductInformation); }
            private set { SetProperty(() => ProductInformation, value); }
        }

        public InvoiceDto Result { get; private set; }

        private IMapper Mapper { get; }

        private IErrorHandler ErrorHandler { get; }

        public static void BuildMetadata(MetadataBuilder<InvoiceDelayViewModel> builder)
        {
            builder.Property(x => x.ExpireReasonId).Required(() => Resources.RequiredErrorMessage);
        }

        protected override async Task HandleLoadedAsync()
        {
            ExpireReasons = Dictionaries.GetItems<ExpireReasonType>()
                .Where(x => x.EntityId == Entity.InvoiceId)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();

            InvoiceDto invoice = await WebClient.ExecuteApiRequestAsync(new QueryInvoice(_parameter.InvoiceId));

            Products = invoice.InvoiceProducts
                .Select(x => Mapper.Map<InvoiceDelayProductViewItem>(x))
                .ToReadOnlyObservableCollection();

            List<ProductAlternativeDto> invoiceAlternatives = await WebClient.ExecuteApiRequestAsync(new QueryInvoiceAlternatives(_parameter.InvoiceId));

            Dictionary<int, (int AlternativeType, int? WarehouseQuantityFree)> productAlternativeDictionary = invoiceAlternatives
                .ToDictionary(x => x.ProductId, x => (x.AlternativeType, x.WarehouseQuantityFree));

            foreach (InvoiceDelayProductViewItem product in Products)
            {
                (int AlternativeType, int? WarehouseQuantityFree) productAlternative = productAlternativeDictionary[product.ProductId];

                product.AlternativeId = productAlternative.AlternativeType;
                product.RemoveSource = product.AlternativeId == (int)ProductAlternativeType.InStock && productAlternative.WarehouseQuantityFree > product.OrderQuantity
                    ? (short)SourceDecision.AutoSource
                    : (short)SourceDecision.SaveSource;
            }

            await base.HandleLoadedAsync();

            Title = "Принятие решения по товарам";
        }

        protected override async Task HandleOkAsync()
        {
            InvoiceDelayProductDto[] products = Products
                .Select(x => new InvoiceDelayProductDto(x.ProductId, x.RemoveSource))
                .ToArray();

            (await ErrorHandler.HandleErrorsAsync(
                    _ => WebClient.ExecuteApiRequestAsync(new DelayInvoice(_parameter.InvoiceId, _parameter.ArriveDate, _parameter.WarehouseId, products, ExpireReasonId!.Value)),
                    "смене даты прибытия",
                    "Дата прибытия изменена",
                    this,
                    true))
                .IfNotNull(x =>
                {
                    Result = x.Data;
                    IsOk = true;
                    Close();
                });
        }

        protected override void OnParameterChanged(object parameter)
        {
            _parameter = (InvoiceDelayParameter)parameter;

            base.OnParameterChanged(parameter);
        }

        private void ProductChanged()
        {
            ProductInformation.ClearProduct();

            if (SelectedProduct != null && SelectedProduct.ProductId != 0)
            {
                ProductInformation.ProductId = new ProductInfoId(SelectedProduct.ProductId, SelectedProduct.CurrencyId);
            }
        }
    }
}