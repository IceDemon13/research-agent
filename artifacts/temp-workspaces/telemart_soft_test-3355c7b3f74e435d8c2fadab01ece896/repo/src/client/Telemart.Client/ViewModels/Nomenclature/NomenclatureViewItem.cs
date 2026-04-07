using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DevExpress.Mvvm;
using Telemart.Client.Business;
using Telemart.Client.Core.Cloning;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.AdditionalService;
using Telemart.Common.Localization;

namespace Telemart.Client.ViewModels.Nomenclature
{
    public sealed class NomenclatureViewItem : BindableBase, IEquatable<NomenclatureViewItem>, ICloneable, ILocalіzableEntity
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public string Name
        {
            get { return GetProperty(() => Name); }
            set { SetProperty(() => Name, value); }
        }

        public string PrefixRu
        {
            get { return GetProperty(() => PrefixRu); }
            set { SetProperty(() => PrefixRu, value); }
        }

        public string PrefixUa
        {
            get { return GetProperty(() => PrefixUa); }
            set { SetProperty(() => PrefixUa, value); }
        }

        public string NameFullRu
        {
            get { return GetProperty(() => NameFullRu); }
            set { SetProperty(() => NameFullRu, value); }
        }

        string ILocalіzableEntity.Name => NameFullRu;

        public string NameFullUa
        {
            get { return GetProperty(() => NameFullUa); }
            set { SetProperty(() => NameFullUa, value); }
        }

        public string NameUkr => NameFullUa;

        public string NameEn => null;

        public int CategoryId
        {
            get { return GetProperty(() => CategoryId); }
            set { SetProperty(() => CategoryId, value); }
        }

        public string Category
        {
            get { return GetProperty(() => Category); }
            set { SetProperty(() => Category, value); }
        }

        public int ParentCategoryId
        {
            get { return GetProperty(() => ParentCategoryId); }
            set { SetProperty(() => ParentCategoryId, value); }
        }

        public string ParentCategoryName
        {
            get { return GetProperty(() => ParentCategoryName); }
            set { SetProperty(() => ParentCategoryName, value); }
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

        public bool KeepSerial
        {
            get { return GetProperty(() => KeepSerial); }
            set { SetProperty(() => KeepSerial, value); }
        }

        public bool AssemblyIncluded
        {
            get { return GetProperty(() => AssemblyIncluded); }
            set { SetProperty(() => AssemblyIncluded, value); }
        }

        public IReadOnlyCollection<ProductPriceSimpleDto> Prices
        {
            get { return GetProperty(() => Prices); }
            set { SetProperty(() => Prices, value); }
        }

        public double Weight
        {
            get { return GetProperty(() => Weight); }
            set { SetProperty(() => Weight, value); }
        }

        public string ProductPn
        {
            get { return GetProperty(() => ProductPn); }
            set { SetProperty(() => ProductPn, value); }
        }

        public int TaxRateId
        {
            get { return GetProperty(() => TaxRateId); }
            set { SetProperty(() => TaxRateId, value); }
        }

        public int WarehouseQuantity
        {
            get { return GetProperty(() => WarehouseQuantity); }
            set { SetProperty(() => WarehouseQuantity, value); }
        }

        public decimal WarehousePrice
        {
            get { return GetProperty(() => WarehousePrice); }
            set { SetProperty(() => WarehousePrice, value); }
        }

        public int WarrantyId
        {
            get { return GetProperty(() => WarrantyId); }
            set { SetProperty(() => WarrantyId, value); }
        }

        public NomenclatureViewItem[] Gifts
        {
            get { return GetProperty(() => Gifts); }
            set { SetProperty(() => Gifts, value); }
        }

        public int Quantity
        {
            get { return GetProperty(() => Quantity); }
            set { SetProperty(() => Quantity, value); }
        }

        public int? MinLeftover
        {
            get { return GetProperty(() => MinLeftover); }
            set { SetProperty(() => MinLeftover, value); }
        }

        public string SegmentName
        {
            get { return GetProperty(() => SegmentName); }
            set { SetProperty(() => SegmentName, value); }
        }

        public int? TradeInSegmentId
        {
            get { return GetProperty(() => TradeInSegmentId); }
            set { SetProperty(() => TradeInSegmentId, value); }
        }

        public string TradeInSegmentName
        {
            get { return GetProperty(() => TradeInSegmentName); }
            set { SetProperty(() => TradeInSegmentName, value); }
        }

        public int? BonusTypeId
        {
            get { return GetProperty(() => BonusTypeId); }
            set { SetProperty(() => BonusTypeId, value); }
        }

        public int TypeId
        {
            get { return GetProperty(() => TypeId); }
            set { SetProperty(() => TypeId, value); }
        }

        public int? MaxBonusesToUse
        {
            get { return GetProperty(() => MaxBonusesToUse); }
            set { SetProperty(() => MaxBonusesToUse, value); }
        }

        public int? BonusesToCharge
        {
            get { return GetProperty(() => BonusesToCharge); }
            set { SetProperty(() => BonusesToCharge, value); }
        }

        public decimal? PriceInUsd
        {
            get { return GetProperty(() => PriceInUsd); }
            set { SetProperty(() => PriceInUsd, value); }
        }

        public decimal? PriceIn
        {
            get { return GetProperty(() => PriceIn); }
            set { SetProperty(() => PriceIn, value); }
        }

        public string Manufactor
        {
            get { return GetProperty(() => Manufactor); }
            set { SetProperty(() => Manufactor, value); }
        }

        public AdditionalServiceCatalogViewItem AdditionalService
        {
            get { return GetProperty(() => AdditionalService); }
            set { SetProperty(() => AdditionalService, value); }
        }

        public ProductAdditionalServiceGroupDto[] AdditionalServiceGroups
        {
            get { return GetProperty(() => AdditionalServiceGroups); }
            set { SetProperty(() => AdditionalServiceGroups, value); }
        }

        public bool FreeDelivery
        {
            get { return GetProperty(() => FreeDelivery); }
            set { SetProperty(() => FreeDelivery, value); }
        }

        public string NameFull => this.GetLocalName(LocalizableNameType.Ukr);

        public static bool operator ==(NomenclatureViewItem left, NomenclatureViewItem right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(NomenclatureViewItem left, NomenclatureViewItem right)
        {
            return !Equals(left, right);
        }

        public bool Equals(NomenclatureViewItem other)
        {
            return Id == other?.Id;
        }

        public override bool Equals(object obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return obj is NomenclatureViewItem item && Equals(item);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        public NomenclatureViewItem Clone()
        {
            NomenclatureViewItem clone = ReflectionObjectCloner.Clone(this);

            clone.Gifts = Gifts?.Select(x => x.Clone()).ToArray();

            return clone;
        }

        public override string ToString()
        {
            return $"{Id.ToString(CultureInfo.InvariantCulture)} {Name} {CurrencyFormatingRules.ToStr(Price, CurrencyId)} {Quantity.ToString(CultureInfo.InvariantCulture)}";
        }
    }
}