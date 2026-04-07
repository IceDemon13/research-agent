using System.Linq;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Validation;
using Telemart.Client.Dictionaries;
using Telemart.Client.Properties;
using Telemart.Common.Constants;

namespace Telemart.Client.ViewModels.Store.Order
{
    internal sealed class OrderViewModelMetadata
    {
        private const string SelectedValueInvalidMessage = "Выбранное значение недопустимо";

        public static void BuildMetadata(MetadataBuilder<OrderViewModel> builder)
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

            builder.Property(x => x.SelectedSubdivision)
                .Required(() => Resources.OrderViewModel_Subdivision);

            builder.Property(x => x.SelectedContractor)
                .Required(() => Resources.OrderViewModel_Contractor)
                .MatchesInstanceRule(
                    (x, y) => (y.State == OrderStatus.Received || y.SelectedContractor != null) && IsSelectedItemValid(x, y),
                    () => Resources.OrderViewModel_Contractor);

            builder.Property(x => x.SelectedPayment)
                .Required(() => Resources.OrderViewModel_PaymentType)
                .MatchesInstanceRule(IsSelectedItemValid, () => SelectedValueInvalidMessage);

            builder.Property(x => x.SelectedCity)
                .Required(() => Resources.OrderViewModel_City)
                .MatchesInstanceRule(IsSelectedItemValid, () => SelectedValueInvalidMessage);

            builder.Property(x => x.SelectedCarryType)
                .Required(() => Resources.OrderViewModel_CarryType)
                .MatchesInstanceRule(IsSelectedItemValid, () => SelectedValueInvalidMessage);

            builder.Property(x => x.SelectedWarehouse)
                .MatchesInstanceRule(
                    (x, y) =>
                        (y.State == OrderStatus.Received || y.SelectedWarehouse != null)
                        && ((y.GetOrderProducts()?.All(p => p.State != OrderProductStatus.Agreed) ?? false) || (x != null && IsSelectedItemValid(x, y)))
                        && IsSelectedItemValid(x, y)
                        && (y.SelectedCarryType?.Id != CarryType.PickupId || x != null),
                    () => Resources.OrderViewModel_Warehouse);

            builder.Property(x => x.DeliveryTime)
                .MatchesInstanceRule(
                    (deliveryTime, order) =>
                        order.State == OrderStatus.Received ||
                        order.IsAllProductsWithNoneOrNoProductSource() ||
                        deliveryTime.HasValue,
                    () => Resources.RequiredErrorMessage);

            builder.Property(x => x.DeliveryTimeTo)
                .MatchesInstanceRule(
                    (deliveryTimeTo, order) =>
                        order.State == OrderStatus.Received ||
                        order.IsAllProductsWithNoneOrNoProductSource() ||
                        deliveryTimeTo.HasValue,
                    () => Resources.RequiredErrorMessage);

            builder.Property(x => x.Address)
                .MatchesInstanceRule(
                    (address, order) => order.SelectedCarryType == null || !string.IsNullOrWhiteSpace(order.DeliveryData.PlaceId) || order.SelectedCarryType.Id == CarryType.PickupId,
                    () => "Выберите адрес")
                .MatchesInstanceRule(
                    isValidFunction: (address, order) => order.SelectedCarryType == null || order.SelectedCarryType.Id != CarryType.UpDeliveryId || !string.IsNullOrEmpty(order.DeliveryData?.Index),
                    () => "Индекс обязателен для этого типа доставки");

            builder.Property(x => x.AssemblyWarehouse)
               .MatchesInstanceRule((x, y) => !y.IsNotReadonlyAssemblyWarehouse || x != null, () => Resources.RequiredErrorMessage);

            builder.Property(x => x.SelectedAdditionalServiceWarehouse)
              .MatchesInstanceRule(
                  (x, y) => !((x is null && y.OrderProducts.Any(z => z.IsAdditionalService && z.ParentRecordId.HasValue && (z.TypeId == ProductType.ServiceId || z.TypeId == ProductType.ServiceCertificateId)))
                    || (x != null && !IsSelectedItemValid(x, y))),
                  () => Resources.RequiredErrorMessage);

            builder.Property(x => x.SelectedOrderSource)
                .Required(() => Resources.OrderViewModel_OrderSource)
                .MatchesInstanceRule(IsSelectedItemValid, () => SelectedValueInvalidMessage);
        }

        private static bool IsSelectedItemValid(ValidatableItem item, OrderViewModel order)
        {
            return item?.Valid ?? true;
        }
    }
}
