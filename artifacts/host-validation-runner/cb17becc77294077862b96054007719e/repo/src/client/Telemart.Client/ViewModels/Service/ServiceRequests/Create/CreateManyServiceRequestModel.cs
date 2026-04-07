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
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Cashbox;
using Telemart.Client.Data.Requests.Features.Contractor;
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
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Common;
using Telemart.Client.ViewModels.Store.Order;
using Telemart.Client.ViewModels.Validation;

namespace Telemart.Client.ViewModels.Service.ServiceRequests.Create
{
    public class CreateManyServiceRequestModel : BindableBase, IDataErrorInfo
    {
        private IReadOnlyCollection<CashboxDto> cashboxes;

        public CreateManyServiceRequestModel(IDictionaries dictionaries, IWebClient webClient, IMapper mapper, IMessageFacadeService messageFacadeService)
        {
            CarryTypes = dictionaries
                .GetItems<CarryType>()
                .Where(x => x.Id is CarryType.PickupId or CarryType.NpWarehouseId or CarryType.NpDeliveryId or CarryType.NpPostBoxId)
                .ToReadOnlyObservableCollection();

            WebClient = webClient;
            Mapper = mapper;
            Dictionaries = dictionaries;
            MessageFacadeService = messageFacadeService;

            Requisites = new RequisitesViewItem();

            SelectedProducts = new ObservableCollection<ServiceRequestProductSnViewItem>();
        }

        #region Order

        public int OrderId
        {
            get { return GetProperty(() => OrderId); }
            set { SetProperty(() => OrderId, value); }
        }

        public OrderDto Order
        {
            get { return GetProperty(() => Order); }
            set { SetProperty(() => Order, value); }
        }

        public int? OrderContractorLimit
        {
            get { return GetProperty(() => OrderContractorLimit); }
            set { SetProperty(() => OrderContractorLimit, value); }
        }

        public int? SearchOrderId
        {
            get { return GetProperty(() => SearchOrderId); }
            set { SetProperty(() => SearchOrderId, value); }
        }

        public int? OrderCityId
        {
            get { return GetProperty(() => OrderCityId); }
            set { SetProperty(() => OrderCityId, value); }
        }

        #endregion

        #region Collections

        public ReadOnlyObservableCollection<ServiceRequestRequirement> ClientRequirements
        {
            get { return GetProperty(() => ClientRequirements); }
            set { SetProperty(() => ClientRequirements, value); }
        }

        public ObservableCollection<ValidationResultItem> ValidationItems
        {
            get { return GetProperty(() => ValidationItems); }
            set { SetProperty(() => ValidationItems, value); }
        }

        public ObservableCollection<ServiceRequestViewItem> CreatedServiceRequests
        {
            get { return GetProperty(() => CreatedServiceRequests); }
            set { SetProperty(() => CreatedServiceRequests, value); }
        }

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

        public ObservableCollection<ServiceRequestProductSnViewItem> SelectedProducts
        {
            get { return GetProperty(() => SelectedProducts); }
            set { SetProperty(() => SelectedProducts, value); }
        }

        #endregion

        #region PersonInfo

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

        public string Email
        {
            get { return GetProperty(() => Email); }
            set { SetProperty(() => Email, value); }
        }

        public ServiceRequestRequirement ClientRequirement
        {
            get { return GetProperty(() => ClientRequirement); }
            set { SetProperty(() => ClientRequirement, value, ClientRequirementChanged); }
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
            set { SetProperty(() => ReturnMoneyPaymentType, value, () => { RaisePropertiesChanged(nameof(Cashbox), nameof(Requisites), nameof(CashboxVisible), nameof(PaymentVisible)); }); }
        }

        public ObservableCollection<ComboBoxItem> Cashboxes
        {
            get { return GetProperty(() => Cashboxes); }
            private set { SetProperty(() => Cashboxes, value); }
        }

        public List<ServiceRequestDto> CreatedServiceRequestsDto { get; set; }

        public bool CashboxVisible => ReturnMoneyPaymentType?.Id == Payment.CashId && !HideRequirementPayment;

        public bool PaymentVisible => !HideRequirementPayment;

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
            set { SetProperty(() => CarryOutId, value, OnCarryOutChanged); }
        }

        public WarehouseDto WarehouseIn
        {
            get { return GetProperty(() => WarehouseIn); }
            set { SetProperty(() => WarehouseIn, value, OnWarehouseInChanged); }
        }

        public DeliveryDataDto DeliveryData
        {
            get { return GetProperty(() => DeliveryData); }
            set { SetProperty(() => DeliveryData, value); }
        }

        public ReadOnlyObservableCollection<WarehouseDto> WarehousesByCarryType
        {
            get { return GetProperty(() => WarehousesByCarryType); }
            set { SetProperty(() => WarehousesByCarryType, value); }
        }

        #endregion

        #region Other

        public int? PaymentId
        {
            get { return GetProperty(() => PaymentId); }
            set { SetProperty(() => PaymentId, value); }
        }

        public int? SubdivisionId
        {
            get { return GetProperty(() => SubdivisionId); }
            set { SetProperty(() => SubdivisionId, value); }
        }

        public int? ContractorId
        {
            get { return GetProperty(() => ContractorId); }
            set { SetProperty(() => ContractorId, value); }
        }

        public bool HideRequirementPayment
        {
            get { return GetProperty(() => HideRequirementPayment); }
            set { SetProperty(() => HideRequirementPayment, value, () => RaisePropertiesChanged(nameof(CashboxVisible))); }
        }

        public bool IsWarrantyRepair
        {
            get { return GetProperty(() => IsWarrantyRepair); }
            set { SetProperty(() => IsWarrantyRepair, value); }
        }

        public bool PaidRepairEnabled
        {
            get { return GetProperty(() => PaidRepairEnabled); }
            set { SetProperty(() => PaidRepairEnabled, value); }
        }

        public bool WarrantyRepairEnabled
        {
            get { return GetProperty(() => WarrantyRepairEnabled); }
            set { SetProperty(() => WarrantyRepairEnabled, value); }
        }

        #endregion

        public IDictionaries Dictionaries { get; }

        string IDataErrorInfo.Error => string.Empty;

        private IWebClient WebClient { get; }

        private IMapper Mapper { get; }

        private IMessageFacadeService MessageFacadeService { get; }

        string IDataErrorInfo.this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        public static void BuildMetadata(MetadataBuilder<CreateManyServiceRequestModel> builder)
        {
            builder.Property(x => x.SearchOrderId).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.Fio).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.Phone).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.ClientRequirement).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.Email).MatchesRegularExpression(@"^$|^.+@.+\..+$", () => Resources.OrderViewModel_Email)
               .MaxLength(100, () => "Значение поля должно быть короче 100 символов");

            builder.Property(x => x.ReturnMoneyPaymentType)
              .MatchesInstanceRule((x, y) => y.ClientRequirement != ServiceRequestRequirement.ReturnMoney || x != null, () => Resources.RequiredErrorMessage);

            builder.Property(x => x.Cashbox)
                .MatchesInstanceRule((x, y) => y.ReturnMoneyPaymentType == null || y.ReturnMoneyPaymentType.Id != Payment.CashId || x.HasValue || y.HideRequirementPayment, () => Resources.RequiredErrorMessage);

            builder.Property(x => x.CityId).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.WarehouseIn).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.CarryInId).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.CarryOutId).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.DeliveryData).MatchesRule((x) => x != null, () => Resources.RequiredErrorMessage);
        }

        public ServiceRequestCreateManyDto GetSaveDto(string sendTo)
        {
            return MapToCreateDto(this, sendTo, new ServiceRequestCreateManyDto());
        }

        public void ReturnMoneyPaymentTypeChanged()
        {
            Cashboxes = cashboxes
                .ForReturn(Currency.Uah.Id, ReturnMoneyPaymentType?.Id)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToObservableCollection();

            Requisites.Enabled = ReturnMoneyPaymentType?.Id > 0 && ReturnMoneyPaymentType.Id == Payment.BankId && !HideRequirementPayment;
            Requisites.Visible = Requisites.Enabled;

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
        }

        public async Task SetOrderDataAsync(IDictionaries dictionaries, OrderDto order)
        {
            PagedResult<ContractorDto> contractorsResult = await WebClient.ExecuteApiRequestAsync(new QueryContractors(), true);
            Contractors = contractorsResult.Data.ToReadOnlyObservableCollection();
            ContractorDto contractor = Contractors.First(x => x.Id == order.ClientId);
            cashboxes = await WebClient.ExecuteApiRequestAsync(new QueryCashboxes(), true);

            if (order.LegalEntity != null)
            {
                cashboxes = cashboxes
                    .ForLegalEntity(order.LegalEntity).ToList();
            }

            OrderId = order.Id;
            Order = order;
            OrderCityId = order.CityId;
            OrderContractorLimit = contractor.Limit;
            SubdivisionId = contractor.SubdivisionId;
            ContractorId = order.ClientId;
            PaymentId = order.PaymentId;
            Fio = order.Fio;
            Phone = order.Phone;
            Email = order.Email;

            HideRequirementPayment = order.IsPartialRefund();

            ReturnMoneyPaymentTypes = GetPaymentTypes(dictionaries, contractor.Limit, order.PaymentId, order.OrderPayments).ToObservableCollection();

            ClientRequirements = dictionaries.GetItemById<Subdivision>(SubdivisionId.Value)
                .GetServiceRequestRequirements()
                .ToReadOnlyObservableCollection();
        }

        public void ClearOrderData()
        {
            ContractorId = null;
            SubdivisionId = null;
            PaymentId = null;
            Fio = null;
            Phone = null;
            Email = null;
            ClientRequirement = null;
            OrderCityId = null;
            OrderContractorLimit = null;
            cashboxes = null;
            ReturnMoneyPaymentTypes = null;
            HideRequirementPayment = false;
        }

        public void CalculateRepairEnabled()
        {
            if (SelectedProducts is null)
            {
                return;
            }

            if (SelectedProducts.All(x => x.WarrantyRemoved))
            {
                IsWarrantyRepair = false;
                PaidRepairEnabled = false;
                WarrantyRepairEnabled = false;
            }
            else
            {
                IsWarrantyRepair = true;
                PaidRepairEnabled = true;
                WarrantyRepairEnabled = true;
            }
        }

        private static IEnumerable<Payment> GetPaymentTypes(IDictionaries dictionaries, int orderContractorLimit, int orderPaymentId, IReadOnlyCollection<OrderPaymentDto> orderPayments)
        {
            if (orderContractorLimit > 1)
            {
                yield return new Payment(-1, "На баланс", null, null, true, false, false, null, false, false, 0, null, 0, false);
            }

            if (orderPayments.Any(x => x.PaymentId == Payment.TerminalId))
            {
                foreach (Payment payment in dictionaries.GetRefundPayments(Payment.CashId, Payment.TerminalId).Where(x => x.Id != Payment.BankId))
                {
                    yield return payment;
                }

                yield break;
            }

            switch (orderPaymentId)
            {
                case Payment.LiqPayId:
                {
                    foreach (Payment payment in dictionaries.GetRefundPayments(Payment.CashId, Payment.LiqPayId).Where(x => x.Id != Payment.BankId))
                    {
                        yield return payment;
                    }

                    yield break;
                }

                case Payment.PortmoneId:
                {
                    foreach (Payment payment in dictionaries.GetRefundPayments(Payment.CashId, Payment.PortmoneId).Where(x => x.Id != Payment.BankId))
                    {
                        yield return payment;
                    }

                    yield break;
                }

                case Payment.CreditId:
                {
                    foreach (Payment payment in dictionaries.GetRefundPayments(Payment.CreditId))
                    {
                        yield return payment;
                    }

                    yield break;
                }

                case Payment.PrivatPartialPayId:
                {
                    foreach (Payment payment in dictionaries.GetRefundPayments(Payment.PrivatPartialPayId))
                    {
                        yield return payment;
                    }

                    yield break;
                }
            }

            foreach (Payment payment in dictionaries.GetRefundPayments(Payment.CashId))
            {
                yield return payment;
            }
        }

        private static ServiceRequestCreateManyProductDto MapToCreateProductDto(ServiceRequestProductSnViewItem source, Payment payment, ComboBoxItem? cashbox, ServiceRequestRequirement requirement, int? serviceRepairTypeId)
        {
            ServiceRequestRequirementBuilder requirementBuilder = new ServiceRequestRequirementBuilder(
               requirement,
               null,
               serviceRepairTypeId,
               source.ChangeOnProductId,
               source.ChangeOnProductName,
               payment,
               cashbox?.DisplayValue ?? string.Empty);

            ServiceRequestCreateManyProductDto target = new ServiceRequestCreateManyProductDto
            {
                ProductId = source.ProductId,
                ProductName = source.Name,
                SerialNumber = source.SerialNumber,
                Defect = source.Defect,
                RequirementText = requirementBuilder.GetRequirementText(),
                ChangeOnProductId = source.ChangeOnProductId,
                BundleId = source.OrderPromoCode?.PromoCodeTypeId == PromoCodeType.Bundle.Id ? source.OrderPromoCode!.PromoCodeId : null
            };

            return target;
        }

        private ServiceRequestCreateManyDto MapToCreateDto(CreateManyServiceRequestModel source, string sendTo, ServiceRequestCreateManyDto target)
        {
            int? serviceRepairTypeId = null;

            if (source.ClientRequirement.Id == ServiceRequestRequirement.RepairId)
            {
                serviceRepairTypeId = IsWarrantyRepair ? ServiceRepairType.Warranty.Id : ServiceRepairType.Paid.Id;
            }

            target.Email = source.Email;
            target.Fio = source.Fio;
            target.Phone = source.Phone;
            target.Phone2 = source.Phone;
            target.OrderId = source.OrderId;
            target.Requisites = Mapper.Map<RefundRequisitesDto>(Requisites);
            target.Requirement = source.ClientRequirement.Id;
            target.RequirementPaymentId = source.ReturnMoneyPaymentType?.Id;
            target.RequirementCashboxId = Cashbox?.Id;
            target.CarryInId = source.CarryInId!.Value;
            target.CarryOutId = source.CarryOutId!.Value;
            target.WarehouseInId = source.WarehouseIn.Id;
            target.CityId = source.CityId;
            target.SendTo = sendTo;
            target.ServiceRepairTypeId = serviceRepairTypeId;
            target.DeliveryDataOut = DeliveryData;

            target.Products = new List<ServiceRequestCreateManyProductDto>();

            foreach (ServiceRequestProductSnViewItem product in source.SelectedProducts)
            {
                target.Products.Add(MapToCreateProductDto(product, source.ReturnMoneyPaymentType, source.Cashbox, source.ClientRequirement, serviceRepairTypeId));
            }

            return target;
        }

        private void ClientRequirementChanged()
        {
            if (SelectedProducts?.Any() != true)
            {
                return;
            }

            if (ClientRequirement != null)
            {
                SelectedProducts.ForEach(x => x.ClientRequirement = ClientRequirement);
            }

            if (ClientRequirement == ServiceRequestRequirement.ReturnMoney)
            {
                int[] bundlePromoCodeIds = SelectedProducts
                    .Select(x => x.OrderPromoCode?.PromoCodeId)
                    .Where(x => x.HasValue)
                    .Select(x => x!.Value)
                    .ToArray();

                if (bundlePromoCodeIds.Length > 0)
                {
                    OrderProductDto[] bundleOrderProducts = Order.Products.Where(x => x.OrderPromoCodeId.HasValue && bundlePromoCodeIds.Contains(x.OrderPromoCodeId!.Value)).ToArray();

                    foreach (OrderProductDto bundleOrderProduct in bundleOrderProducts)
                    {
                        int selectedProductIdRowsQuantity = SelectedProducts.Count(x => x.ProductId == bundleOrderProduct.Product.Id);

                        if (bundleOrderProduct.SerialNumbers?.Any() == true)
                        {
                            int leftProductIdQuantity = bundleOrderProduct.SerialNumbers.Count - selectedProductIdRowsQuantity;

                            foreach (ServiceRequestSerialNumberViewItem bundleSerialNumber in bundleOrderProduct.SerialNumbers.Select(x => new ServiceRequestSerialNumberViewItem(x.SerialNumber, x.WarrantyRemoved)))
                            {
                                if (leftProductIdQuantity <= 0)
                                {
                                    break;
                                }

                                if (SelectedProducts.All(x => x.SerialNumber != bundleSerialNumber.SerialNumber))
                                {
                                    SelectedProducts.Add(
                                        new ServiceRequestProductSnViewItem
                                        {
                                            ProductId = bundleOrderProduct.Product.Id,
                                            Name = bundleOrderProduct.Product.NameFullUkr,
                                            OrderPromoCode = Order.PromoCodes?.FirstOrDefault(x => x.PromoCodeId == bundleOrderProduct.OrderPromoCodeId),
                                            AddedForBundleMoneyReturn = true,
                                            SerialNumber = null,
                                            SerialNumbers = bundleSerialNumber.Yield().ToReadOnlyObservableCollection()
                                        });

                                    leftProductIdQuantity--;
                                }
                            }
                        }
                        else
                        {
                            int leftProductIdQuantity = bundleOrderProduct.Quantity - selectedProductIdRowsQuantity;

                            foreach (int _ in Enumerable.Range(0, bundleOrderProduct.Quantity))
                            {
                                if (leftProductIdQuantity <= 0)
                                {
                                    break;
                                }

                                SelectedProducts.Add(
                                    new ServiceRequestProductSnViewItem
                                    {
                                        ProductId = bundleOrderProduct.Product.Id,
                                        Name = bundleOrderProduct.Product.NameFullUkr,
                                        OrderPromoCode = Order.PromoCodes?.FirstOrDefault(x => x.PromoCodeId == bundleOrderProduct.OrderPromoCodeId),
                                        AddedForBundleMoneyReturn = true,
                                        SerialNumber = null,
                                        SerialNumbers = null
                                    });

                                leftProductIdQuantity--;
                            }
                        }
                    }

                    if (SelectedProducts.Any(x => x.AddedForBundleMoneyReturn))
                    {
                        string messageText = $"Вместе с сервисным товаром вам должны для возврата ДС принести весь бандл: {Environment.NewLine}{Environment.NewLine}"
                                             + string.Join($"{Environment.NewLine}{Environment.NewLine}", bundleOrderProducts.Select(x => $"{x.Product.NameFullUkr} ({x.Quantity} шт.)"));

                        MessageFacadeService.ShowMessageBoxInfo(messageText);
                    }
                }
            }
            else
            {
                SelectedProducts = SelectedProducts.Where(x => !x.AddedForBundleMoneyReturn).ToObservableCollection();
            }
        }

        private void OnCarryOutChanged()
        {
            DeliveryData = null;
            OnWarehouseInChanged();
            RaisePropertiesChanged(nameof(DeliveryData), nameof(CityId));
        }

        private void OnWarehouseInChanged()
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