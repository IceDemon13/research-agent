using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Business.Order;
using Telemart.Client.Business.ServiceRequest;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Data.Requests.Features.Novaposhta;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Helpers;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Cashbox;
using Telemart.Client.TransferObjects.City;
using Telemart.Client.TransferObjects.Novaposhta;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Common;
using Telemart.Client.ViewModels.Store.Order;
using Telemart.Client.ViewModels.Validation;

namespace Telemart.Client.ViewModels.Service.ServiceRequests.Create
{
    public sealed class CreateServiceRequestModel : BindableBase, IDataErrorInfo
    {
        private readonly IMapper _mapper;
        private readonly Dictionary<int, List<OrderProductSnDto>> _productSerials = new Dictionary<int, List<OrderProductSnDto>>();
        private Dictionary<int, ProductAttributesDto> _attributes;
        private IReadOnlyCollection<CashboxDto> _cashboxes;

        public CreateServiceRequestModel(IDictionaries dictionaries, IMapper mapper)
        {
            _mapper = mapper;
            SearchMode = SearchOrderMode.OrderNumber;

            CarryTypes = dictionaries
                .GetItems<CarryType>()
                .Where(x => x.Id is CarryType.PickupId or CarryType.NpWarehouseId or CarryType.NpDeliveryId or CarryType.NpPostBoxId)
                .ToReadOnlyObservableCollection();

            Requisites = new RequisitesViewItem();
        }

        #region ViewModelLogic

        public SearchOrderMode SearchMode
        {
            get { return GetProperty(() => SearchMode); }
            set { SetProperty(() => SearchMode, value, () => { RaisePropertiesChanged(nameof(SearchOrderId), nameof(SearchSerialNumber)); }); }
        }

        public int? SearchOrderId
        {
            get { return GetProperty(() => SearchOrderId); }
            set { SetProperty(() => SearchOrderId, value); }
        }

        public string SearchSerialNumber
        {
            get { return GetProperty(() => SearchSerialNumber); }
            set { SetProperty(() => SearchSerialNumber, value); }
        }

        public int? SearchProductId
        {
            get { return GetProperty(() => SearchProductId); }
            set { SetProperty(() => SearchProductId, value); }
        }

        public string SearchProductName
        {
            get { return GetProperty(() => SearchProductName); }
            set { SetProperty(() => SearchProductName, value); }
        }

        public int? SearchContractorId
        {
            get { return GetProperty(() => SearchContractorId); }
            set { SetProperty(() => SearchContractorId, value); }
        }

        public bool HideRequirementPayment
        {
            get { return GetProperty(() => HideRequirementPayment); }
            private set { SetProperty(() => HideRequirementPayment, value); }
        }

        #endregion

        #region Product

        public int OrderId
        {
            get { return GetProperty(() => OrderId); }
            private set { SetProperty(() => OrderId, value); }
        }

        public int? GroupId
        {
            get { return GetProperty(() => GroupId); }
            set { SetProperty(() => GroupId, value); }
        }

        public int? OrderCityId
        {
            get { return GetProperty(() => OrderCityId); }
            private set { SetProperty(() => OrderCityId, value); }
        }

        public int? ContractorId
        {
            get { return GetProperty(() => ContractorId); }
            set { SetProperty(() => ContractorId, value); }
        }

        public int? OrderContractorLimit
        {
            get { return GetProperty(() => OrderContractorLimit); }
            private set { SetProperty(() => OrderContractorLimit, value); }
        }

        public int? SubdivisionId
        {
            get { return GetProperty(() => SubdivisionId); }
            private set { SetProperty(() => SubdivisionId, value, () => { RaisePropertyChanged(nameof(SerialNumber)); }); }
        }

        public int? ProductId
        {
            get
            {
                return GetProperty(() => ProductId);
            }

            set
            {
                SetProperty(
                    () => ProductId,
                    value,
                    () =>
                    {
                        ProductKeepSerial = ProductId.HasValue
                            ? _attributes[ProductId.Value].KeepSerial
                            : null;

                        RaisePropertyChanged(nameof(SerialNumber));
                    });
            }
        }

        public bool? ProductKeepSerial
        {
            get { return GetProperty(() => ProductKeepSerial); }
            private set { SetProperty(() => ProductKeepSerial, value); }
        }

        public OrderProductSnDto SerialNumber
        {
            get { return GetProperty(() => SerialNumber); }
            set { SetProperty(() => SerialNumber, value); }
        }

        public ObservableCollection<OrderProductSnDto> SerialNumbers
        {
            get { return GetProperty(() => SerialNumbers); }
            private set { SetProperty(() => SerialNumbers, value); }
        }

        public string StatedDefect
        {
            get { return GetProperty(() => StatedDefect); }
            set { SetProperty(() => StatedDefect, value); }
        }

        public ObservableCollection<ComboBoxItem> Products
        {
            get { return GetProperty(() => Products); }
            private set { SetProperty(() => Products, value); }
        }

        #endregion

        #region Declarant

        public ServiceRequestRequirement ClientRequirement
        {
            get { return GetProperty(() => ClientRequirement); }
            set { SetProperty(() => ClientRequirement, value, () => RaisePropertyChanged(nameof(StatedDefect))); }
        }

        public string Email
        {
            get { return GetProperty(() => Email); }
            set { SetProperty(() => Email, value); }
        }

        public string Fio
        {
            get { return GetProperty(() => Fio); }
            set { SetProperty(() => Fio, value); }
        }

        public string Phone
        {
            get { return GetProperty(() => Phone); }
            set { SetProperty(() => Phone, value); }
        }

        public string Phone2
        {
            get { return GetProperty(() => Phone2); }
            set { SetProperty(() => Phone2, value); }
        }

        public int? PaymentId
        {
            get { return GetProperty(() => PaymentId); }
            set { SetProperty(() => PaymentId, value); }
        }

        public int? CustomerId
        {
            get { return GetProperty(() => CustomerId); }
            set { SetProperty(() => CustomerId, value); }
        }

        #endregion

        #region ReturnMoney

        public ComboBoxItem? Cashbox
        {
            get { return GetProperty(() => Cashbox); }
            set { SetProperty(() => Cashbox, value); }
        }

        public RequisitesViewItem Requisites
        {
            get { return GetProperty(() => Requisites); }
            set { SetProperty(() => Requisites, value); }
        }

        public ObservableCollection<Payment> ReturnMoneyPaymentTypes
        {
            get { return GetProperty(() => ReturnMoneyPaymentTypes); }
            private set { SetProperty(() => ReturnMoneyPaymentTypes, value); }
        }

        public Payment ReturnMoneyPaymentType
        {
            get { return GetProperty(() => ReturnMoneyPaymentType); }
            set { SetProperty(() => ReturnMoneyPaymentType, value, ReturnMoneyPaymentTypeChanged); }
        }

        public ObservableCollection<ComboBoxItem> Cashboxes
        {
            get { return GetProperty(() => Cashboxes); }
            private set { SetProperty(() => Cashboxes, value); }
        }

        public bool CashboxVisible => ReturnMoneyPaymentType?.Id == Payment.CashId;

        #endregion

        #region Repair

        public bool IsWarrantyRepair
        {
            get { return GetProperty(() => IsWarrantyRepair); }
            set { SetProperty(() => IsWarrantyRepair, value); }
        }

        public bool WarrantyRepairEnabled
        {
            get { return GetProperty(() => WarrantyRepairEnabled); }
            private set { SetProperty(() => WarrantyRepairEnabled, value); }
        }

        public bool PaidRepairEnabled
        {
            get { return GetProperty(() => PaidRepairEnabled); }
            private set { SetProperty(() => PaidRepairEnabled, value); }
        }

        #endregion

        #region Change

        public int? ChangeOnProductId
        {
            get { return GetProperty(() => ChangeOnProductId); }
            set { SetProperty(() => ChangeOnProductId, value); }
        }

        public string ChangeOnProductName
        {
            get { return GetProperty(() => ChangeOnProductName); }
            set { SetProperty(() => ChangeOnProductName, value); }
        }

        #endregion

        #region Logistics

        public int? CityId
        {
            get { return GetProperty(() => CityId); }
            set { SetProperty(() => CityId, value, () => { DeliveryData = null; }); }
        }

        public int? CarryInId
        {
            get { return GetProperty(() => CarryInId); }
            set { SetProperty(() => CarryInId, value); }
        }

        public int? CarryOutId
        {
            get { return GetProperty(() => CarryOutId); }
            set { SetProperty(() => CarryOutId, value, CarryOutChanged); }
        }

        public WarehouseDto WarehouseIn
        {
            get { return GetProperty(() => WarehouseIn); }
            set { SetProperty(() => WarehouseIn, value, WarehouseInChanged); }
        }

        public DeliveryDataDto DeliveryData
        {
            get { return GetProperty(() => DeliveryData); }
            set { SetProperty(() => DeliveryData, value); }
        }

        #endregion

        #region Data

        public ReadOnlyObservableCollection<CarryType> CarryTypes
        {
            get { return GetProperty(() => CarryTypes); }
            private set { SetProperty(() => CarryTypes, value); }
        }

        public ReadOnlyObservableCollection<ContractorDto> Contractors
        {
            get { return GetProperty(() => Contractors); }
            set { SetProperty(() => Contractors, value); }
        }

        public ReadOnlyObservableCollection<WarehouseDto> Warehouses
        {
            get { return GetProperty(() => Warehouses); }
            set { SetProperty(() => Warehouses, value); }
        }

        public ReadOnlyObservableCollection<CityDto> Cities
        {
            get { return GetProperty(() => Cities); }
            set { SetProperty(() => Cities, value); }
        }

        public ReadOnlyObservableCollection<WarehouseDto> WarehousesByCarryType
        {
            get { return GetProperty(() => WarehousesByCarryType); }
            set { SetProperty(() => WarehousesByCarryType, value); }
        }

        #endregion

        #region ErrorInfo

        public ServiceRequestDto Result
        {
            get { return GetProperty(() => Result); }
            set { SetProperty(() => Result, value); }
        }

        public ObservableCollection<ValidationResultItem> ValidationItems
        {
            get { return GetProperty(() => ValidationItems); }
            set { SetProperty(() => ValidationItems, value); }
        }

        string IDataErrorInfo.Error => string.Empty;

        string IDataErrorInfo.this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        #endregion

        public static void BuildMetadata(MetadataBuilder<CreateServiceRequestModel> builder)
        {
            builder.Property(x => x.SearchOrderId)
                .MatchesInstanceRule(
                    (x, y) => y.SearchMode != SearchOrderMode.OrderNumber || x.HasValue,
                    () => Resources.RequiredErrorMessage);
            builder.Property(x => x.SearchSerialNumber)
                .MatchesInstanceRule(
                    (x, y) => y.SearchMode != SearchOrderMode.SerialNumber || !string.IsNullOrWhiteSpace(x),
                    () => Resources.RequiredErrorMessage);

            builder.Property(x => x.ProductId).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.SerialNumber)
                .MatchesInstanceRule(
                    (x, y) => y.SubdivisionId != Subdivision.Wholesale.Id || y.ProductId == null || y.ProductKeepSerial == null || !y.ProductKeepSerial.Value || !string.IsNullOrWhiteSpace(x?.SerialNumber),
                    () => Resources.RequiredErrorMessage);

            builder.Property(x => x.StatedDefect)
                .MatchesInstanceRule((x, y) => y.ClientRequirement is null || y.ClientRequirement == ServiceRequestRequirement.TradeIn || x != null, () => Resources.RequiredErrorMessage);

            builder.Property(x => x.SubdivisionId).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.ContractorId).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.Fio).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.Phone).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.ClientRequirement).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.Email).MatchesRegularExpression(@"^$|^.+@.+\..+$", () => Resources.OrderViewModel_Email)
                .MaxLength(100, () => "Значение поля должно быть короче 100 символов");

            builder.Property(x => x.ReturnMoneyPaymentType)
                .MatchesInstanceRule((x, y) => y.ClientRequirement != ServiceRequestRequirement.ReturnMoney || x != null, () => Resources.RequiredErrorMessage);

            builder.Property(x => x.Cashbox)
                .MatchesInstanceRule((x, y) => y.ReturnMoneyPaymentType == null || y.ReturnMoneyPaymentType.Id != Payment.CashId || x.HasValue, () => Resources.RequiredErrorMessage);

            builder.Property(x => x.CityId).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.WarehouseIn).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.CarryInId).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.CarryOutId).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.DeliveryData).MatchesRule(x => x != null, () => Resources.RequiredErrorMessage);
        }

        public bool SelectProductDataValid()
        {
            return ProductId.HasValue
                && ProductKeepSerial.HasValue
                && (SubdivisionId != Subdivision.Wholesale.Id || !ProductKeepSerial.Value || !string.IsNullOrWhiteSpace(SerialNumber?.SerialNumber));
        }

        public void ClearOrderData()
        {
            ProductId = null;
            Products = null;
            SerialNumber = null;
            SerialNumbers = null;
            StatedDefect = null;

            ContractorId = null;
            SubdivisionId = null;
            PaymentId = null;
            Fio = null;
            Phone = null;
            Phone2 = null;
            Email = null;
            ClientRequirement = null;

            ReturnMoneyPaymentType = null;
            Cashbox = null;
            Requisites = new RequisitesViewItem();

            ChangeOnProductId = null;
            ChangeOnProductName = null;

            CityId = null;
            WarehouseIn = null;
            CarryInId = null;
            CarryOutId = null;
            DeliveryData = null;

            _attributes = null;
            OrderCityId = null;
            OrderContractorLimit = null;

            _cashboxes = null;
            Cashboxes = null;
            ReturnMoneyPaymentTypes = null;
            ReturnMoneyPaymentType = null;
            HideRequirementPayment = false;
        }

        public ServiceRequestCreateDto GetSaveDto(string sendTo)
        {
            return MapToSaveDto(this, sendTo, new ServiceRequestCreateDto(), IsWarrantyRepair);
        }

        public void SetOrderData(
            IDictionaries dictionaries,
            OrderDto order,
            ContractorDto orderContractor,
            IReadOnlyCollection<ProductAttributesDto> productAttributes,
            IReadOnlyCollection<CashboxDto> cashboxList)
        {
            _attributes = productAttributes.ToDictionary(x => x.ProductId);
            _cashboxes = cashboxList;

            OrderId = order.Id;
            OrderCityId = order.CityId;
            OrderContractorLimit = orderContractor.Limit;
            SubdivisionId = orderContractor.SubdivisionId;
            ContractorId = order.ClientId;
            PaymentId = order.PaymentId;
            Fio = order.Fio;
            Phone = order.Phone;
            Phone2 = order.Phone2;
            Email = order.Email;

            ReturnMoneyPaymentTypes = dictionaries.GetServiceRequestPaymentTypes(order.PaymentId, order.OrderPayments, orderContractor.Limit).ToObservableCollection();

            HideRequirementPayment = order.IsPartialRefund();

            SetProducts(order.Products);
        }

        public void SetSerials(int productId)
        {
            if (_productSerials.TryGetValue(productId, out List<OrderProductSnDto> serials))
            {
                SerialNumbers = serials.ToObservableCollection();
            }
        }

        public void CalculateRepairsEnabled()
        {
            if (SerialNumber is null || !SerialNumber.WarrantyRemoved)
            {
                PaidRepairEnabled = true;
                WarrantyRepairEnabled = true;
                IsWarrantyRepair = true;
            }
            else
            {
                WarrantyRepairEnabled = false;
                IsWarrantyRepair = false;
                PaidRepairEnabled = false;
            }
        }

        private ServiceRequestCreateDto MapToSaveDto(CreateServiceRequestModel source, string sendTo, ServiceRequestCreateDto target, bool isWarrantyRepair)
        {
            int? repairTypeId = null;
            if (source.ClientRequirement == ServiceRequestRequirement.Repair)
            {
                repairTypeId = isWarrantyRepair ? ServiceRepairType.Warranty.Id : ServiceRepairType.Paid.Id;
            }

            ServiceRequestRequirementBuilder requirementBuilder = new ServiceRequestRequirementBuilder(
                source.ClientRequirement,
                null,
                repairTypeId,
                source.ChangeOnProductId,
                source.ChangeOnProductName,
                source.ReturnMoneyPaymentType,
                source.Cashbox?.DisplayValue ?? string.Empty);

            target.OrderId = source.OrderId;
            target.ProductId = source.ProductId!.Value;
            target.SerialNumber = source.SerialNumber?.SerialNumber;
            target.StatedDefect = source.StatedDefect;
            target.CustomerId = source.CustomerId;
            target.Fio = source.Fio;
            target.Phone = source.Phone;
            target.Phone2 = source.Phone2;
            target.Email = source.Email;
            target.Requisites = _mapper.Map<RefundRequisitesDto>(Requisites);
            target.Requirement = source.ClientRequirement.Id;
            target.GroupId = source.GroupId;
            target.RequirementText = requirementBuilder.GetRequirementText();

            switch (source.ClientRequirement.Id)
            {
                case ServiceRequestRequirement.ChangeId:
                    target.ProductNewId = source.ChangeOnProductId;
                    break;
                case ServiceRequestRequirement.ReturnMoneyId:

                    if (!HideRequirementPayment)
                    {
                        int? paymentId = source.ReturnMoneyPaymentType.Id > 0
                            ? source.ReturnMoneyPaymentType.Id
                            : null;

                        target.RequirementPaymentId = paymentId;
                        target.RequirementCashboxId = paymentId == Payment.CashId
                            ? source.Cashbox!.Value.Id
                            : null;
                    }

                    break;
                case ServiceRequestRequirement.RepairId:
                    target.ServiceRepairTypeId = repairTypeId;
                    break;
            }

            target.CityId = source.CityId!.Value;
            target.WarehouseInId = source.WarehouseIn.Id;
            target.CarryInId = source.CarryInId!.Value;
            target.CarryOutId = source.CarryOutId!.Value;
            target.DeliveryDataOut = source.DeliveryData;

            target.SendTo = sendTo;

            return target;
        }

        private void SetProducts(IEnumerable<OrderProductDto> orderProducts)
        {
            Products = new ObservableCollection<ComboBoxItem>();

            _productSerials.Clear();

            foreach (OrderProductDto orderProduct in orderProducts)
            {
                int productId = orderProduct.Product.Id;

                if (!_productSerials.TryGetValue(productId, out List<OrderProductSnDto> serials))
                {
                    serials = new List<OrderProductSnDto>();
                    _productSerials.Add(productId, serials);

                    Products.Add(new ComboBoxItem(productId, orderProduct.Product.Name));
                }

                serials.AddRange(orderProduct.SerialNumbers);
            }
        }

        private void ReturnMoneyPaymentTypeChanged()
        {
            Requisites.Enabled = ReturnMoneyPaymentType?.Id > 0 && ReturnMoneyPaymentType.Id != Payment.CashId;
            Requisites.Visible = Requisites.Enabled;

            Cashboxes = _cashboxes
                .ForReturn(Currency.Uah.Id, ReturnMoneyPaymentType?.Id)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToObservableCollection();

            if (Requisites.Visible)
            {
                Fio fio = new(Fio);

                Requisites.FirstName = fio.FirstName;
                Requisites.LastName = fio.LastName;
                Requisites.MiddleName = fio.MiddleName;
            }
            else
            {
                Requisites = new RequisitesViewItem();
            }

            RaisePropertiesChanged(nameof(Cashbox), nameof(Requisites), nameof(CashboxVisible));

            if (!CashboxVisible)
            {
                Cashbox = null;
            }
        }

        private void CarryOutChanged()
        {
            DeliveryData = null;

            WarehouseInChanged();

            RaisePropertiesChanged(nameof(DeliveryData), nameof(CityId));
        }

        private void WarehouseInChanged()
        {
            if (CarryOutId != CarryType.PickupId)
            {
                return;
            }

            if (WarehouseIn != null)
            {
                DeliveryData = new DeliveryDataDto
                {
                    CityId = WarehouseIn.CityId.ToString(),
                    PlaceId = WarehouseIn.Id.ToString(),
                    Street = null,
                    House = null,
                    Flat = null,
                    Extra = null,
                    MaxAllowedWeight = WarehouseIn.MaxPackageWeight,
                    Address = WarehouseIn.Address,
                    AddressUkr = WarehouseIn.AddressUa,
                    AddressEn = WarehouseIn.AddressEn
                };
            }
            else
            {
                DeliveryData = null;
            }
        }
    }
}