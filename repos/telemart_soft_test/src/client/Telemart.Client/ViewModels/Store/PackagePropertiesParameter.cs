using System.Collections.Generic;

namespace Telemart.Client.ViewModels.Store
{
    internal sealed class PackagePropertiesParameter
    {
        public PackagePropertiesParameter(
            int places,
            decimal insurance,
            int carryId,
            decimal? totalWeight = null,
            PackageMaxDimensionsParameter maxDimensionsParameter = null,
            IReadOnlyCollection<PackagePropertiesProductParameter> products = null,
            bool allowEditInsurance = false)
        {
            Products = products;
            Places = places;
            Insurance = insurance;
            CarryId = carryId;
            TotalWeight = totalWeight;
            MaxDimensionsParameter = maxDimensionsParameter;
            AllowEditInsurance = allowEditInsurance;
        }

        public int Places { get; }

        public decimal Insurance { get; }

        public int CarryId { get; }

        public decimal? TotalWeight { get; }

        public bool AllowEditInsurance { get; }

        public PackageMaxDimensionsParameter MaxDimensionsParameter { get; }

        public IReadOnlyCollection<PackagePropertiesProductParameter> Products { get; }
    }
}