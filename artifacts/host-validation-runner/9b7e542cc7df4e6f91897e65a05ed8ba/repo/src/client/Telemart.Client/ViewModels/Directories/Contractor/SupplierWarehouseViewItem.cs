using System;
using System.Collections.ObjectModel;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Directories.Contractor
{
    public sealed class SupplierWarehouseViewItem : TelemartEditorViewItemBase
    {
        public string Name
        {
            get { return GetProperty(() => Name); }
            set { SetProperty(() => Name, value); }
        }

        public int SupplierId
        {
            get { return GetProperty(() => SupplierId); }
            set { SetProperty(() => SupplierId, value); }
        }

        public int? CityId
        {
            get { return GetProperty(() => CityId); }
            set { SetProperty(() => CityId, value); }
        }

        public string ShortName
        {
            get { return GetProperty(() => ShortName); }
            set { SetProperty(() => ShortName, value); }
        }

        public string Pricer24Id
        {
            get { return GetProperty(() => Pricer24Id); }
            set { SetProperty(() => Pricer24Id, value); }
        }

        public ObservableCollection<string> SupplierWarehouseAvails
        {
            get { return GetProperty(() => SupplierWarehouseAvails); }
            set { SetProperty(() => SupplierWarehouseAvails, value); }
        }

        public bool Active
        {
            get { return GetProperty(() => Active); }
            set { SetProperty(() => Active, value); }
        }

        public static void BuildMetadata(MetadataBuilder<SupplierWarehouseViewItem> builder)
        {
            builder.Property(x => x.Name)
                .Required(() => Resources.RequiredErrorMessage)
                .MaxLength(20);

            builder.Property(x => x.CityId)
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.ShortName)
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.Pricer24Id)
                .MatchesRule(x => string.IsNullOrEmpty(x) || Guid.TryParseExact(x, "D", out _), () => Resources.RequiredErrorMessage);
        }
    }
}