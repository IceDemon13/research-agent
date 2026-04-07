using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Catalog;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Nomenclature;
using Telemart.Common.Helpers;

namespace Telemart.Client.ViewModels.AdditionalService
{
    public sealed class SelectAdditionalServicesViewModel : TelemartDialogViewModelBase
    {
        private SelectAdditionalServicesParameter _parameter;

        public SelectAdditionalServicesViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Mapper = mapper;

            SelectedAdditionalServices = new ObservableCollection<AdditionalServiceCatalogViewItem>();
            AdditionalServices = new ObservableCollection<AdditionalServiceCatalogViewItem>();

            SelectedAdditionalServices.CollectionChanged += SelectedAdditionalServicesViewItemCollectionChanged;
        }

        public SelectAdditionalServicesViewModel()
        {
        }

        public List<NomenclatureViewItem> Products { get; set; }

        public ObservableCollection<AdditionalServiceCatalogViewItem> SelectedAdditionalServices
        {
            get { return GetProperty(() => SelectedAdditionalServices); }
            set { SetProperty(() => SelectedAdditionalServices, value); }
        }

        public string InfoLabel
        {
            get { return GetProperty(() => InfoLabel); }
            private set { SetProperty(() => InfoLabel, value); }
        }

        public string InfoLabelToolTip
        {
            get { return GetProperty(() => InfoLabelToolTip); }
            private set { SetProperty(() => InfoLabelToolTip, value); }
        }

        public bool AllowEdit
        {
            get { return GetProperty(() => AllowEdit); }
            private set { SetProperty(() => AllowEdit, value); }
        }

        public string ProductName
        {
            get { return GetProperty(() => ProductName); }
            set { SetProperty(() => ProductName, value); }
        }

        public int PaymentId
        {
            get { return GetProperty(() => PaymentId); }
            set { SetProperty(() => PaymentId, value); }
        }

        public IReadOnlyCollection<ExternalPaymentDto> ExternalPayments
        {
            get { return GetProperty(() => ExternalPayments); }
            set { SetProperty(() => ExternalPayments, value); }
        }

        public ObservableCollection<AdditionalServiceCatalogViewItem> AdditionalServices
        {
            get { return GetProperty(() => AdditionalServices); }
            private set { SetProperty(() => AdditionalServices, value); }
        }

        private IMapper Mapper { get; }

        protected override async Task HandleLoadedAsync()
        {
            _parameter = (SelectAdditionalServicesParameter)Parameter;
            ProductName = _parameter.ProductName;
            PaymentId = _parameter.PaymentId;
            ExternalPayments = _parameter.ExternalPayments;
            AllowEdit = _parameter.AllowEdit;

            if (AllowEdit)
            {
                Title = "Выбор услуг";
            }
            else
            {
                Title = "Просмотр услуг";
                InfoLabel = "Выбор услуг запрещен.";
                InfoLabelToolTip = "Для добавления услуг откройте заказ в режиме редактирования.";
            }

            QueryProductByIdsDto queryProductByIdsDto = new QueryProductByIdsDto(new[] { _parameter.ProductId }, _parameter.ContractorId, queryAdditionalServices: true);

            List<ProductDto> products = await WebClient.ExecuteCatalogApiRequestAsync(new QueryProductByIds(queryProductByIdsDto));

            ProductDto product = products.FirstOrDefault();

            if (product != null)
            {
                AdditionalServices = GetAdditionalServices(product.AdditionalServiceGroups)
                    .Where(x => _parameter.AdditionalServiceProductTypeIds is null || _parameter.AdditionalServiceProductTypeIds.Contains(x.ProductTypeId))
                    .Select(x => new AdditionalServiceCatalogViewItem()
                    {
                        Id = x.Id,
                        ProductId = x.ProductId,
                        ProductName = x.ProductName,
                        ProductNameUkr = x.ProductNameUkr,
                        ProductNameEn = x.ProductNameEn,
                        Name = x.Name,
                        Price = PriceHelper.CalculatePriceForAdditionalService(_parameter.ProductPriceOut, x.Percent, x.MinPrice),
                        MinPrice = x.MinPrice,
                        ProductTypeId = x.ProductTypeId,
                        AdditionalWarranty = x.AdditionalWarranty,
                        ProductLink = x.ProductLink,
                        AssemblyPart = x.AssemblyPart,
                        AutoAdd = x.AutoAdd,
                        Percent = x.Percent
                    })
                    .OrderBy(x => x.Price)
                    .ToObservableCollection();

                if (AdditionalServices.Count == 0)
                {
                    MessageFacadeService.ShowNotificationInfo("Для выбранного товара отсутствуют подходящие услуги");

                    Close();
                }
            }

            if (ExternalPayments?.Any(
                    x =>
                    {
                        Payment payment = Dictionaries.GetItemById<Payment>(x.PaymentId);

                        return !Payment.IsEditingAllowed(
                            x.PaymentId,
                            x.PaymentStateId,
                            payment.Credit,
                            payment.PartialCredit);
                    }) == true)
            {
                MessageFacadeService.ShowNotificationInfo("Кредитный заказ. Добавление услуг запрещено");
                Close();
                return;
            }

            await base.HandleLoadedAsync();
        }

        protected override async Task HandleOkAsync()
        {
            if (AllowEdit)
            {
                if (ExternalPayments?.Any(
                        x =>
                        {
                            Payment payment = Dictionaries.GetItemById<Payment>(x.PaymentId);

                            return !Payment.IsEditingAllowed(
                                x.PaymentId,
                                x.PaymentStateId,
                                payment.Credit,
                                payment.PartialCredit);
                        }) == true)
                {
                    MessageFacadeService.ShowNotificationWarning("Нельзя добавлять услугу для кредитных заказов");
                    return;
                }

                if (!SelectedAdditionalServices.Any())
                {
                    MessageFacadeService.ShowNotificationWarning("Ничего не выбрано");
                    return;
                }

                List<ProductDto> productDtos = await WebClient.ExecuteCatalogApiRequestAsync(new QueryProductByIds(
                    _parameter.ContractorId,
                    SelectedAdditionalServices.Select(x => x.ProductId).ToArray(),
                    true));

                Products = productDtos.Select(x => Mapper.Map<NomenclatureViewItem>(x)).ToList();
                Products.ForEach(x =>
                {
                    x.TypeId = SelectedAdditionalServices.First(y => y.ProductId == x.Id).ProductTypeId;
                    x.Quantity = 1;
                    x.AdditionalService = SelectedAdditionalServices.FirstOrDefault(y => y.ProductId == x.Id);
                    x.Price = AdditionalServices.First(y => y.ProductId == x.Id).Price;
                });

                IsOk = true;
            }

            Close();
        }

        private static IEnumerable<ProductAdditionalServiceDto> GetAdditionalServices(IEnumerable<ProductAdditionalServiceGroupDto> groups)
        {
            if (groups != null)
            {
                foreach (ProductAdditionalServiceGroupDto group in groups)
                {
                    foreach (ProductAdditionalServiceDto productAdditionalService in GetAdditionalServices(group.Groups))
                    {
                        yield return productAdditionalService;
                    }

                    if (group.AdditionalServices != null)
                    {
                        foreach (ProductAdditionalServiceDto productAdditionalService in group.AdditionalServices)
                        {
                            yield return productAdditionalService;
                        }
                    }
                }
            }
        }

        private void SelectedAdditionalServicesViewItemCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (AllowEdit)
            {
                InfoLabel = $"Услуг на сумму: {SelectedAdditionalServices.Sum(x => x.Price)} грн";
            }
        }
    }
}