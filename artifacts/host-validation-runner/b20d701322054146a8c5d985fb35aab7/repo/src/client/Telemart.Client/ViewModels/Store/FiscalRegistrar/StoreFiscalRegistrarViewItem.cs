using System.ComponentModel;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Properties;
using Telemart.Common.Dictionaries;

namespace Telemart.Client.ViewModels.Store.FiscalRegistrar
{
    public sealed class StoreFiscalRegistrarViewItem : BindableBase, IDataErrorInfo
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public string NameFullUa
        {
            get { return GetProperty(() => NameFullUa); }
            set { SetProperty(() => NameFullUa, value); }
        }

        public string Barcode
        {
            get { return GetProperty(() => Barcode); }
            set { SetProperty(() => Barcode, value); }
        }

        public int Quantity
        {
            get { return GetProperty(() => Quantity); }
            set { SetProperty(() => Quantity, value); }
        }

        public decimal Price
        {
            get { return GetProperty(() => Price); }
            set { SetProperty(() => Price, value); }
        }

        public ProductType ProductType
        {
            get { return GetProperty(() => ProductType); }
            set { SetProperty(() => ProductType, value); }
        }

        #region IDataErrorInfo

        string IDataErrorInfo.Error => string.Empty;

        string IDataErrorInfo.this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        #endregion

        public static void BuildMetadata(MetadataBuilder<StoreFiscalRegistrarViewItem> builder)
        {
            builder.Property(x => x.Quantity)
                .MatchesRule(x => x > 0, () => Resources.RequiredErrorMessage);

            builder.Property(x => x.Price)
                .MatchesRule(x => x > 0, () => Resources.RequiredErrorMessage);
        }
    }
}