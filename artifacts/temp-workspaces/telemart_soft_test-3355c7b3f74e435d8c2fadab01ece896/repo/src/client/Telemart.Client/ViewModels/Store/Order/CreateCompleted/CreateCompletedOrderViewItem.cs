using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Common;
using Telemart.Client.Common.Validation;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Store.Order.OrderPaymentInfo;
using Telemart.Common.Constants;

namespace Telemart.Client.ViewModels.Store.Order.CreateCompleted
{
    public class CreateCompletedOrderViewItem : TelemartViewItemBase, IOrderPaymentInfo
    {
        public CreateCompletedOrderViewItem()
        {
            OrderProducts = new ObservableCollection<CreateCompletedOrderProductViewItem>();
            PromoCodes = new ObservableCollection<OrderPromoCodeDto>();
        }

        public int? PackageDeliveryCost { get; set; }

        public int? MinPriceFreeDelivery { get; set; }

        public decimal? MoneyBackAmount { get; set; }

        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public string LastName
        {
            get { return GetProperty(() => LastName); }
            set { SetProperty(() => LastName, value); }
        }

        public string FirstName
        {
            get { return GetProperty(() => FirstName); }
            set { SetProperty(() => FirstName, value); }
        }

        public string MiddleName
        {
            get { return GetProperty(() => MiddleName); }
            set { SetProperty(() => MiddleName, value); }
        }

        public string Phone
        {
            get { return GetProperty(() => Phone); }
            set { SetProperty(() => Phone, value, PhoneChanged); }
        }

        public string Phone2
        {
            get { return GetProperty(() => Phone2); }
            set { SetProperty(() => Phone2, value); }
        }

        public string ValidatedPhone
        {
            get { return GetProperty(() => ValidatedPhone); }
            set { SetProperty(() => ValidatedPhone, value); }
        }

        public List<CustomerBonusDto> CustomerBonuses
        {
            get { return GetProperty(() => CustomerBonuses); }
            set { SetProperty(() => CustomerBonuses, value, () => RaisePropertyChanged(nameof(BonusVisible))); }
        }

        public int? BonusesToChargeQuantity
        {
            get { return GetProperty(() => BonusesToChargeQuantity); }
            set { SetProperty(() => BonusesToChargeQuantity, value); }
        }

        public string Email
        {
            get { return GetProperty(() => Email); }
            set { SetProperty(() => Email, value); }
        }

        public string Comment
        {
            get { return GetProperty(() => Comment); }
            set { SetProperty(() => Comment, value); }
        }

        public OrderContractorViewItem SelectedContractor
        {
            get { return GetProperty(() => SelectedContractor); }
            set { SetProperty(() => SelectedContractor, value); }
        }

        public OrderCityViewItem SelectedCity
        {
            get { return GetProperty(() => SelectedCity); }
            set { SetProperty(() => SelectedCity, value); }
        }

        public OrderCarryTypeViewItem SelectedCarryType
        {
            get { return GetProperty(() => SelectedCarryType); }
            set { SetProperty(() => SelectedCarryType, value, SelectedCarryTypeChanged); }
        }

        public OrderWarehouseViewItem SelectedWarehouse
        {
            get { return GetProperty(() => SelectedWarehouse); }
            set { SetProperty(() => SelectedWarehouse, value, SelectedWarehouseChanged); }
        }

        public OrderPaymentViewItem SelectedPayment
        {
            get { return GetProperty(() => SelectedPayment); }
            set { SetProperty(() => SelectedPayment, value); }
        }

        public string Address
        {
            get { return GetProperty(() => Address); }
            set { SetProperty(() => Address, value); }
        }

        public bool SeparateWarrantyCards
        {
            get { return GetProperty(() => SeparateWarrantyCards); }
            set { SetProperty(() => SeparateWarrantyCards, value); }
        }

        public DeliveryDataDto DeliveryData
        {
            get { return GetProperty(() => DeliveryData); }
            set { SetProperty(() => DeliveryData, value, () => RaisePropertiesChanged(nameof(Address))); }
        }

        public ObservableCollection<CreateCompletedOrderProductViewItem> OrderProducts
        {
            get { return GetProperty(() => OrderProducts); }
            set { SetProperty(() => OrderProducts, value); }
        }

        public ObservableCollection<OrderPromoCodeDto> PromoCodes
        {
            get { return GetProperty(() => PromoCodes); }
            set { SetProperty(() => PromoCodes, value); }
        }

        public static void BuildMetadata(MetadataBuilder<CreateCompletedOrderViewItem> builder)
        {
            builder.Property(x => x.LastName)
                .ApplyFioPartValidationRules()
                .MatchesInstanceRule(
                    (x, y) => y.SelectedCarryType == null || !y.SelectedCarryType.RequireLastName || !string.IsNullOrWhiteSpace(x),
                    () => Resources.RequiredErrorMessage);

            builder.Property(x => x.FirstName)
                .ApplyFioPartValidationRules()
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.MiddleName)
                .ApplyFioPartValidationRules()
                .MatchesInstanceRule(
                    (x, y) => y.SelectedCarryType == null || !y.SelectedCarryType.RequireMiddleName || !string.IsNullOrWhiteSpace(x),
                    () => Resources.RequiredErrorMessage);

            builder.Property(x => x.Phone)
                .Required(() => Resources.OrderViewModel_Phone1);

            // empty string or email
            builder.Property(x => x.Email)
                .MatchesRegularExpression(RegexConstants.EmailRegex, () => Resources.OrderViewModel_Email);

            builder.Property(x => x.SelectedContractor)
                .Required(() => Resources.OrderViewModel_Contractor)
                .MatchesRule(
                    x => x?.Valid ?? true,
                    () => Resources.OrderViewModel_Contractor);

            builder.Property(x => x.SelectedPayment)
                .Required(() => Resources.OrderViewModel_PaymentType)
                .MatchesRule(x => x?.Valid ?? true, () => "Выбранное значение недопустимо");

            builder.Property(x => x.SelectedCity)
                .Required(() => Resources.OrderViewModel_City)
                .MatchesRule(x => x?.Valid ?? true, () => "Выбранное значение недопустимо");

            builder.Property(x => x.SelectedCarryType)
                .Required(() => Resources.OrderViewModel_CarryType)
                .MatchesRule(x => x?.Valid ?? true, () => "Выбранное значение недопустимо");

            builder.Property(x => x.SelectedWarehouse)
                .Required(() => Resources.OrderViewModel_Warehouse)
                .MatchesRule(x => x?.Valid ?? true, () => "Выбранное значение недопустимо");
        }

        public IEnumerable<IOrderPaymentInfoProduct> GetOrderProducts()
        {
            return OrderProducts;
        }

        public IEnumerable<IOrderPayment> GetOrderPayments()
        {
            yield break;
        }

        public int GetBonusesQuantity()
        {
            return OrderProducts?.Sum(x => x.AppliedBonusesQuantity) ?? 0;
        }

        public int GetBonusesToChargeQuantity()
        {
            return BonusesToChargeQuantity ?? 0;
        }

        public void RaiseProperties()
        {
            RaisePropertiesChanged(nameof(SelectedContractor), nameof(SelectedPayment), nameof(SelectedCity), nameof(SelectedCarryType), nameof(SelectedWarehouse));
        }

        public void RaiseProperties(params string[] properties)
        {
            RaisePropertiesChanged(properties);
        }

        public bool BonusVisible => CustomerBonuses?.Any(x => x.Quantity > 0) == true;

        private void SelectedWarehouseChanged()
        {
            if (SelectedWarehouse != null)
            {
                Address = SelectedWarehouse.Address;

                DeliveryData = new DeliveryDataDto
                {
                    CityId = SelectedWarehouse.CityId.ToString(),
                    PlaceId = SelectedWarehouse.Id.ToString(),
                    Street = null,
                    House = null,
                    Flat = null,
                    Extra = null,
                    MaxAllowedWeight = SelectedWarehouse.MaxPackageWeight,
                    Address = SelectedWarehouse.Address,
                    AddressUkr = SelectedWarehouse.AddressUa,
                    AddressEn = SelectedWarehouse.AddressEn
                };

                RaisePropertyChanged(nameof(Address));
            }
        }

        private void SelectedCarryTypeChanged()
        {
            PackageDeliveryCost = SelectedCarryType?.DeliveryCost;
            MinPriceFreeDelivery = SelectedCarryType?.MinFreeDeliveryCost;

            RaisePropertiesChanged(nameof(LastName), nameof(MiddleName), nameof(Address));
        }

        private void PhoneChanged()
        {
            if (Phone != ValidatedPhone)
            {
                CustomerBonuses?.Clear();
                RaisePropertyChanged(nameof(BonusVisible));
            }
        }
    }
}