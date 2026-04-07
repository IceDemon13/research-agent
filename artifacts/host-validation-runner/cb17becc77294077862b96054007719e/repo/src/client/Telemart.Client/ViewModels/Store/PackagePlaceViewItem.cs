using System.ComponentModel;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Business.Delivery;
using Telemart.Client.Common.Validation;
using Telemart.Client.Dictionaries;
using Telemart.Client.Properties;

namespace Telemart.Client.ViewModels.Store
{
    internal sealed class PackagePlaceViewItem : BindableBase, IDataErrorInfo, ICarrySupport
    {
        public PackagePlaceViewItem(int number, decimal weight, decimal insurance, int carryId, int? length = null, int? width = null, int? height = null)
        {
            Number = number;
            Weight = weight;
            Insurance = insurance;
            CarryId = carryId;
            Length = length;
            Width = width;
            Height = height;
        }

        public int Number
        {
            get { return GetProperty(() => Number); }
            set { SetProperty(() => Number, value); }
        }

        public int CarryId
        {
            get { return GetProperty(() => CarryId); }
            set { SetProperty(() => CarryId, value, () => RaisePropertyChanged(nameof(Length))); }
        }

        public decimal Weight
        {
            get { return GetProperty(() => Weight); }
            set { SetProperty(() => Weight, value); }
        }

        public decimal Insurance
        {
            get { return GetProperty(() => Insurance); }
            set { SetProperty(() => Insurance, value); }
        }

        public int? Length
        {
            get { return GetProperty(() => Length); }
            set { SetProperty(() => Length, value); }
        }

        public int? Width
        {
            get { return GetProperty(() => Width); }
            set { SetProperty(() => Width, value); }
        }

        public int? Height
        {
            get { return GetProperty(() => Height); }
            set { SetProperty(() => Height, value); }
        }

        #region IDataErrorInfo

        public string Error => string.Empty;

        public string this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        #endregion

        public static void BuildMetadata(MetadataBuilder<PackagePlaceViewItem> builder)
        {
            builder.Property(x => x.Insurance)
                .MatchesRule(x => x > 0, () => Resources.RequiredErrorMessage);
            builder.Property(x => x.Weight).PackageWeight();

            builder.Property(x => x.Height)
                .MatchesInstanceRule((x, y) => x != null || (y.CarryId != CarryType.NpPostBoxId && y.CarryId != CarryType.UpDeliveryId && y.CarryId != CarryType.UpWarehouseId), () => Resources.RequiredErrorMessage);
            builder.Property(x => x.Width)
                .MatchesInstanceRule((x, y) => x != null || (y.CarryId != CarryType.NpPostBoxId && y.CarryId != CarryType.UpDeliveryId && y.CarryId != CarryType.UpWarehouseId), () => Resources.RequiredErrorMessage);
            builder.Property(x => x.Length)
                .MatchesInstanceRule((x, y) => x != null || (y.CarryId != CarryType.NpPostBoxId && y.CarryId != CarryType.UpDeliveryId && y.CarryId != CarryType.UpWarehouseId), () => Resources.RequiredErrorMessage);

            builder.Property(x => x.Height).NpPostBoxHeight();
            builder.Property(x => x.Width).NpPostBoxWidth();
            builder.Property(x => x.Length).PlaceLength();
        }
    }
}