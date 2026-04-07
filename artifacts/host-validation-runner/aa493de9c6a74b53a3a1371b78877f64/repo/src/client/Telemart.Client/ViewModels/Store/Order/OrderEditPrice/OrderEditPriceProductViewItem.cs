using System.ComponentModel;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Business;
using Telemart.Client.Business.Order;
using Telemart.Client.Common;
using Telemart.Client.Dictionaries;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Store.Order.OrderPaymentInfo;

namespace Telemart.Client.ViewModels.Store.Order.OrderEditPrice
{
    public class OrderEditPriceProductViewItem : BindableBase, IDataErrorInfo, IOrderProduct
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public int ProductId
        {
            get { return GetProperty(() => ProductId); }
            set { SetProperty(() => ProductId, value); }
        }

        public string ProductName
        {
            get { return GetProperty(() => ProductName); }
            set { SetProperty(() => ProductName, value); }
        }

        public int ProductTypeId
        {
            get { return GetProperty(() => ProductTypeId); }
            set { SetProperty(() => ProductTypeId, value); }
        }

        public int CurrencyId
        {
            get { return GetProperty(() => CurrencyId); }
            set { SetProperty(() => CurrencyId, value); }
        }

        public decimal Price
        {
            get { return GetProperty(() => Price); }
            set { SetProperty(() => Price, value); }
        }

        public decimal PriceOut
        {
            get { return GetProperty(() => PriceOut); }
            set { SetProperty(() => PriceOut, value); }
        }

        public int CurrencyOutId
        {
            get { return GetProperty(() => CurrencyOutId); }
            set { SetProperty(() => CurrencyOutId, value); }
        }

        public OrderProductStatus State
        {
            get { return GetProperty(() => State); }
            set { SetProperty(() => State, value); }
        }

        public int Quantity
        {
            get { return GetProperty(() => Quantity); }
            set { SetProperty(() => Quantity, value); }
        }

        public OrderProductSource Source
        {
            get { return GetProperty(() => Source); }
            set { SetProperty(() => Source, value); }
        }

        public decimal? NewPrice
        {
            get { return GetProperty(() => NewPrice); }
            set { SetProperty(() => NewPrice, value); }
        }

        public int? OrderFolderId
        {
            get { return GetProperty(() => OrderFolderId); }
            set { SetProperty(() => OrderFolderId, value); }
        }

        public int? ParentRecordId
        {
            get { return GetProperty(() => ParentRecordId); }
            set { SetProperty(() => ParentRecordId, value); }
        }

        public bool IsAdditionalService
        {
            get { return GetProperty(() => IsAdditionalService); }
            set { SetProperty(() => IsAdditionalService, value); }
        }

        public bool AssemblyIncluded
        {
            get { return GetProperty(() => AssemblyIncluded); }
            set { SetProperty(() => AssemblyIncluded, value); }
        }

        public int? AssemblyQuantity { get; set; }

        public bool AllowEdit => CurrencyOutId == Currency.Uah.Id && ProductTypeId != ProductType.GuestProductId;

        string IDataErrorInfo.Error => string.Empty;

        Price IOrderPaymentInfoProduct.OriginalPrice => new Price(Price, CurrencyId);

        Price IOrderPaymentInfoProduct.Price => new Price(NewPrice ?? PriceOut, CurrencyOutId);

        string IDataErrorInfo.this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        public static void BuildMetadata(MetadataBuilder<OrderEditPriceProductViewItem> builder)
        {
            builder.Property(x => x.NewPrice).MatchesInstanceRule(
                (x, y) => y.ProductTypeId == ProductType.GuestProductId || (x > 0 && x < Constants.MaxProductPrice),
                () => Resources.RequiredErrorMessage);
        }
    }
}