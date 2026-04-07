using System.Collections.Generic;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.ViewModels.Base;
using Telemart.Common.Localization;

namespace Telemart.Client.ViewModels.AssemblyService
{
    public class AssemblyServiceProductViewItem : TelemartViewItemBase, ILocalіzableEntity
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public int AssemblyServiceId
        {
            get { return GetProperty(() => AssemblyServiceId); }
            set { SetProperty(() => AssemblyServiceId, value); }
        }

        public CategoryType CategoryType
        {
            get { return GetProperty(() => CategoryType); }
            set { SetProperty(() => CategoryType, value); }
        }

        public bool KeepSerialOverridden
        {
            get { return GetProperty(() => KeepSerialOverridden); }
            set { SetProperty(() => KeepSerialOverridden, value); }
        }

        public bool KeepSerial
        {
            get { return GetProperty(() => KeepSerial); }
            set { SetProperty(() => KeepSerial, value, () => KeepSerialOverridden = value); }
        }

        public int? OrderProductId
        {
            get { return GetProperty(() => OrderProductId); }
            set { SetProperty(() => OrderProductId, value); }
        }

        public int ProductId
        {
            get { return GetProperty(() => ProductId); }
            set { SetProperty(() => ProductId, value); }
        }

        public int ScannedQuantity
        {
            get { return GetProperty(() => ScannedQuantity); }
            set { SetProperty(() => ScannedQuantity, value, () => RaisePropertiesChanged(nameof(Deviation), nameof(HasDeviation))); }
        }

        public List<string> SerialNumbers
        {
            get { return GetProperty(() => SerialNumbers); }
            set { SetProperty(() => SerialNumbers, value); }
        }

        public int Quantity
        {
            get { return GetProperty(() => Quantity); }
            set { SetProperty(() => Quantity, value, () => RaisePropertiesChanged(nameof(Deviation), nameof(HasDeviation))); }
        }

        public int Deviation => ScannedQuantity - Quantity;

        public bool HasDeviation => Deviation != 0;

        public string DisplayName => this.GetLocalName(LocalizableNameType.Ukr);

        public string Name
        {
            get { return GetProperty(() => Name); }
            set { SetProperty(() => Name, value, () => RaisePropertyChanged(nameof(DisplayName))); }
        }

        public string NameUkr
        {
            get { return GetProperty(() => NameUkr); }
            set { SetProperty(() => NameUkr, value, () => RaisePropertyChanged(nameof(DisplayName))); }
        }

        public string NameEn
        {
            get { return GetProperty(() => NameEn); }
            set { SetProperty(() => NameEn, value, () => RaisePropertyChanged(nameof(DisplayName))); }
        }

        public string FullName
        {
            get { return GetProperty(() => FullName); }
            set { SetProperty(() => FullName, value); }
        }
    }
}